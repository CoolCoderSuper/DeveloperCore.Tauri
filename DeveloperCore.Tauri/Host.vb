Imports System.Reflection
Imports System.Reflection.Emit
Imports System.Text
Imports System.Text.Json
Imports WatsonWebsocket
'TODO: BCL proxy objects?
'TODO: Proxy object disposal

Public Class Host
    Private ReadOnly _server As WatsonWsServer
    Private ReadOnly _handlers As New List(Of ProxyEventInfo)
    Private Shared ReadOnly JsonSerializerOptions As New JsonSerializerOptions With {.PropertyNamingPolicy = JsonNamingPolicy.CamelCase}

    Public Sub New(host As String, port As Integer)
        _server = New WatsonWsServer(host, port, False)
        AddHandler _server.MessageReceived, AddressOf Server_MessageReceived
    End Sub

    Public ReadOnly Property Commands As New Dictionary(Of String, [Delegate])
    Public ReadOnly Property TypeAliases As New Dictionary(Of String, Type)
    Public ReadOnly Property Instances As New List(Of ProxyObject)

    Public Sub Start()
        _server.Start()
    End Sub

    Public Sub [Stop]()
        _server.Stop()
    End Sub
    
    Public Async Function Cleanup() As Task
        For Each client As ClientMetadata In _server.ListClients()
            Dim response As New InvocationResponse With {.Id = GetUniqueKey(), .Type = "cleanup", .Result = Instances.Select(Function(x) x.Id).ToArray()}
            Dim value As String = JsonSerializer.Serialize(response, JsonSerializerOptions)
            Await _server.SendAsync(client.Guid, value)
        Next
    End Function

    Private Async Sub Server_MessageReceived(sender As Object, e As MessageReceivedEventArgs)
        Dim req As InvocationRequest = JsonSerializer.Deserialize(Of InvocationRequest)(Encoding.UTF8.GetString(e.Data), JsonSerializerOptions)
        Dim response As New InvocationResponse With {.Id = req.Id, .Type = "response"}
        Try
            If req.Type = "create" Then
                Dim type As Type = Nothing
                If Not TypeAliases.TryGetValue(req.Name, type) Then
                    type = Type.GetType(req.Name)
                End If
                If type Is Nothing Then
                    response.Error = "Type not found"
                Else
                    Dim instance As Object = Activator.CreateInstance(type)
                    instance.Id = GetUniqueKey()
                    Instances.Add(instance)
                    response.Result = instance
                End If
            ElseIf req.Type = "cleanup" Then
                'TODO: Listen for all clients
                Dim ids As String() = JsonSerializer.Deserialize(Of String())(req.Arguments.First().Value)
                If ids.Any() Then
                    Instances.RemoveAll(Function(x) ids.Contains(x.Id))
                    GC.Collect()
                    GC.WaitForPendingFinalizers()
                    GC.Collect()
                End If
            ElseIf req.InstanceId <> Nothing Then
                Dim instance As ProxyObject = Instances.FirstOrDefault(Function(x) x.Id = req.InstanceId)
                If instance Is Nothing Then
                End If
                If req.Type = "method" Then
                    Dim method As MethodInfo = instance.GetType().GetMethod(req.Name)
                    If method Is Nothing Then
                        response.Error = "Method not found"
                    Else
                        response.Result = Await CallMethod(method, instance, req.Arguments)
                    End If
                ElseIf req.Type = "property" Then
                    Dim prop As PropertyInfo = instance.GetType().GetProperty(req.Name)
                    If prop Is Nothing Then
                        response.Error = "Property not found"
                    Else
                        If req.Arguments.Count = 0 Then
                            response.Result = GetProxy(prop.GetValue(instance))
                        Else
                            Dim arg As Object = JsonSerializer.Deserialize(req.Arguments.First().Value, prop.PropertyType, JsonSerializerOptions)
                            If prop.PropertyType.IsSubclassOf(GetType(ProxyObject)) Then
                                arg = GetProxy(arg)
                            End If
                            prop.SetValue(instance, arg)
                        End If
                    End If
                ElseIf req.Type = "event" Then
                    Dim [event] As EventInfo = instance.GetType().GetEvent(req.Name)
                    If [event] Is Nothing Then
                        response.Error = "Event not found"
                    Else
                        If req.Action = "add" Then
                            Dim id As String = GetUniqueKey()
                            Dim moduleBuilder As ModuleBuilder = AssemblyBuilder.DefineDynamicAssembly(New AssemblyName("DynamicAssembly"), AssemblyBuilderAccess.Run).DefineDynamicModule("DynamicModule")
                            Dim wrapperBuilder As TypeBuilder = moduleBuilder.DefineType("EventWrapper", TypeAttributes.Public)
                            Dim field As FieldBuilder = wrapperBuilder.DefineField("Host", GetType(Host), FieldAttributes.Private Or FieldAttributes.InitOnly)
                            Dim constructor As ConstructorBuilder = wrapperBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, {GetType(Host)})
                            Dim constructorIl As ILGenerator = constructor.GetILGenerator()
                            With constructorIl
                                .Emit(OpCodes.Ldarg_0)
                                .Emit(OpCodes.Call, GetType(Object).GetConstructor(Type.EmptyTypes))
                                .Emit(OpCodes.Ldarg_0)
                                .Emit(OpCodes.Ldarg_1)
                                .Emit(OpCodes.Stfld, field)
                                .Emit(OpCodes.Ret)
                            End With
                            Dim params As ParameterInfo() = [event].EventHandlerType.GetMethod("Invoke").GetParameters()
                            Dim handlerBuilder As MethodBuilder = wrapperBuilder.DefineMethod("HandleEvent", MethodAttributes.Public, Nothing, params.Select(Function(x) x.ParameterType).ToArray)
                            Dim handlerIl As ILGenerator = handlerBuilder.GetILGenerator()
                            With handlerIl
                                .Emit(OpCodes.Ldarg_0)
                                .Emit(OpCodes.Ldfld, field)
                                .Emit(OpCodes.Ldstr, id)
                                .Emit(OpCodes.Ldc_I4, params.Length)
                                .Emit(OpCodes.Newarr, GetType(Object))
                                For Each param In params
                                    .Emit(OpCodes.Dup)
                                    .Emit(OpCodes.Ldc_I4, param.Position)
                                    .Emit(OpCodes.Ldarg, param.Position + 1)
                                    If param.ParameterType.IsValueType Then
                                        .Emit(OpCodes.Box, param.ParameterType)
                                    End If
                                    .Emit(OpCodes.Stelem_Ref)
                                Next
                                .Emit(OpCodes.Callvirt, GetType(Host).GetMethod("HandleEvent"))
                                .Emit(OpCodes.Ret)
                            End With
                            Dim wrapperType As Type = wrapperBuilder.CreateType()
                            Dim wrapperInstance As Object = Activator.CreateInstance(wrapperType, {Me})
                            Dim handlerMethod As MethodInfo = wrapperType.GetMethod("HandleEvent")
                            Dim handler As [Delegate] = handlerMethod.CreateDelegate([event].EventHandlerType, wrapperInstance)
                            [event].AddEventHandler(instance, handler)
                            Dim info As New ProxyEventInfo(id, req.Name, e.Client.Guid, handler)
                            _handlers.Add(info)
                            response.Result = id
                        ElseIf req.Action = "remove" Then
                            Dim id As String = JsonSerializer.Deserialize(Of String)(req.Arguments.First().Value)
                            Dim info As ProxyEventInfo = _handlers.First(Function(x) x.Id = id)
                            [event].RemoveEventHandler(instance, info.Handler)
                            _handlers.Remove(info)
                        Else
                            response.Error = "Invalid action"
                        End If
                    End If
                Else
                    response.Error = "Invalid type"
                End If
            Else
                Dim [delegate] As [Delegate] = Commands(req.Name)
                response.Result = Await CallMethod([delegate], req.Arguments)
            End If
        Catch ex As Exception
            response.Error = ex.ToString
        End Try
        If IsProxyObject(response.Result) Then
            response.IsProxy = True
        End If
        Dim value As String = JsonSerializer.Serialize(response, JsonSerializerOptions)
        Await _server.SendAsync(e.Client.Guid, value)
    End Sub

    Public Async Sub HandleEvent(eventId As String, params As Object())
        Dim info As ProxyEventInfo = _handlers.First(Function(x) x.Id = eventId)
        Dim response As New InvocationResponse With {.Id = GetUniqueKey(), .Result = New EventFireInfo(info.Id, params.Select(Function(x) New EventFireParam(x, IsProxyObject(x))).ToArray()), .Type = "event"}
        Dim value As String = JsonSerializer.Serialize(response, JsonSerializerOptions)
        Await _server.SendAsync(info.Guid, value)
    End Sub

    Private Async Function CallMethod([delegate] As [Delegate], args As Dictionary(Of String, Object)) As Task(Of Object)
        Dim result As Object = [delegate].DynamicInvoke(GetArgs([delegate].Method, args))
        Return Await ProcessValue(result)
    End Function

    Private Async Function CallMethod(method As MethodInfo, instance As Object, args As Dictionary(Of String, Object)) As Task(Of Object)
        Dim result As Object = method.Invoke(instance, GetArgs(method, args))
        Return Await ProcessValue(result)
    End Function

    Private Async Function ProcessValue(result As Object) As Task(Of Object)
        If result IsNot Nothing AndAlso (result.GetType() = GetType(Task) OrElse result.GetType().IsSubclassOf(GetType(Task))) Then
            Dim task As Task = result
            Await task
            result = task.GetType().GetProperty("Result").GetValue(task)
        End If
        result = GetProxy(result)
        Return result
    End Function

    Private Function GetArgs(method As MethodInfo, args As Dictionary(Of String, Object)) As Object()
        Dim actualArgs As New List(Of Object)
        For Each param In method.GetParameters()
            Dim arg As Object = JsonSerializer.Deserialize(args(param.Name), param.ParameterType, JsonSerializerOptions)
            If param.ParameterType.IsSubclassOf(GetType(ProxyObject)) Then
                actualArgs.Add(GetProxy(arg))
            Else
                actualArgs.Add(arg)
            End If
        Next
        Return actualArgs.ToArray
    End Function

    Private Function GetProxy(result As Object) As Object
        If IsProxyObject(result) Then
            If Instances.Any(Function(x) x.Id = result.Id) Then
                result = Instances.First(Function(x) x.Id = result.Id)
            Else
                result.Id = GetUniqueKey(20)
                Instances.Add(result)
            End If
        End If
        Return result
    End Function

    Private Shared Function IsProxyObject(value As Object) As Boolean
        Return value IsNot Nothing AndAlso value.GetType().IsSubclassOf(GetType(ProxyObject))
    End Function

    Private Shared Function GetUniqueKey() As String
        Const a As String = "ABCDEFGHJKLMNOPQRSTUVWXYZ234567890"
        Dim data As Byte() = New Byte((a.Length) - 1) {}
        Random.Shared.NextBytes(data)
        Dim result As New StringBuilder(20)
        For Each b As Byte In data
            result.Append(a(b Mod (a.Length - 1)))
        Next
        Return result.ToString
    End Function
End Class