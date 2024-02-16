export type InvocationType = 'create' | 'method' | 'property' | 'event' | 'cleanup'

export type InvocationRequest = {
    id: string
    instanceId?: string
    type: InvocationType
    name: string
    action: string
    arguments: any
}

export type InvocationResponse = {
    id: string
    result: any
    isProxy: boolean
    type: 'response' | 'event' | 'cleanup'
    error: string
}
