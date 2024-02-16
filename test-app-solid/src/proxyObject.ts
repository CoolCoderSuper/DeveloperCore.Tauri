import { TauriConnector } from './tauriConnector';

export type ProxyConstructor<T> = (id: string, connector: TauriConnector) => T

export abstract class ProxyObject {
    id: string
    connector: TauriConnector

    constructor(id: string, connector: TauriConnector) {
        this.id = id
        this.connector = connector
    }

    async invoke<T>(command: string, args: any, constructor?: ProxyConstructor<T>): Promise<T> {
        return this.connector.command<T>(command, this, args, 'method', constructor)
    }

    async get<T>(command: string, constructor?: ProxyConstructor<T>): Promise<T> {
        return this.connector.command<T>(command, this, {}, 'property', constructor)
    }

    async set(command: string, value: any): Promise<void> {
        return this.connector.command<void>(command, this, {value}, 'property')
    }

    async bind(event: string, callback: Function, constructors: ProxyConstructor<any>[]): Promise<ProxyEvent> {
        return this.connector.bind(this, event, callback, constructors)
    }

    async unbind(event: ProxyEvent): Promise<void> {
        return this.connector.unbind(this, event)
    }
}

export type ProxyEvent = {
    id: string
    name: string
    callback: Function
    paramConstructors: ProxyConstructor<any>[]
}
