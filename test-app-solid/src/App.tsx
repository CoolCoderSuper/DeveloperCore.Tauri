import './App.css'
import { TauriConnector } from './tauriConnector.ts';
import { ProxyEvent } from './proxyObject.ts';
import { TestInput } from './testInput.ts';

function App() {
    async function bind() {
        const res = await connector.invoke("test2", {}, TestInput.create)
        event = await res.bindTestEvent((sender, e) => {
            console.log('Event fired', sender)
        })
        event2 = await res.bindTestEvent2((sender, e) => {
            console.log('Event fired 2', sender)
        })
    }

    async function test() {
        /*const res = await connector.invoke("test2", {}, TestInput.create)
        await res.raiseTestEvent()
        await res.raiseTestEvent2()*/
        const res = await connector.invoke("test2", {}, TestInput.create)
        alert(await res.toString())
        console.log(await res.updateName("test"))
        alert(await res.name)
        await res.setName("Joe")
        alert(await res.name)
        alert('Child testing')
        const child = await res.child
        alert(await child.name)
        await child.setName("Bob")
        alert(await child.name)
        alert('Child set')
        const newChild = await connector.create("TestInput", TestInput.create)
        alert(await newChild.name)
        await newChild.setName("New Child")
        alert(await newChild.name)
        await child.setChild(newChild)
        alert('Async test')
        const asyncRes = await connector.invoke("test3", {a: res}, TestInput.create)
        alert(await asyncRes.name)
    }

    async function unbind() {
        const res = await connector.invoke("test2", {}, TestInput.create)
        await res.unbindTestEvent(event)
        await res.unbindTestEvent2(event2)
    }

    async function cleanup() {
        await connector.invoke("cleanup", {})
    }

    return (
        <>
            <button onClick={bind}>Bind</button>
            <button onClick={test}>Test</button>
            <button onClick={unbind}>Unbind</button>
            <button onClick={cleanup}>Cleanup</button>
        </>
    )
}

export default App

const connector = new TauriConnector("localhost", 5178)
let event: ProxyEvent
let event2: ProxyEvent
