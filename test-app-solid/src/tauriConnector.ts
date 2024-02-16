import type { ProxyConstructor, ProxyEvent, ProxyObject } from './proxyObject';
import type { InvocationRequest, InvocationResponse, InvocationType } from './invocation';

export class TauriConnector {
    ws: WebSocket
    private requestQueue: Map<string, (response: InvocationResponse) => void> = new Map()
    private handlers: ProxyEvent[] = []
    private proxies: { id: string, proxy: WeakRef<ProxyObject> }[] = []

    constructor(host: string, port: number) {
        this.ws = new WebSocket(`ws://${host}:${port}`)
        this.ws.onmessage = this.onMessage.bind(this)
    }

    private async onMessage(event: MessageEvent) {
        const response: InvocationResponse = JSON.parse(event.data)
        if (response.type === 'event') {
            const event = this.handlers.find(e => e.id === response.result.id)
            if (event) {
                let args = []
                let constructorIndex = 0
                for (const param of response.result.params) {
                    if (param.isProxy) {
                        const proxy = event.paramConstructors[constructorIndex](param.value.id, this)
                        args.push(proxy)
                        this.proxies.push({id: proxy.id, proxy: new WeakRef<ProxyObject>(proxy)})
                        constructorIndex++
                    } else {
                        args.push(param)
                    }
                }
                event.callback(...args)
            }
        } else if (response.type === 'cleanup') {
            console.log('Cleaning up dead proxies', this.proxies)
            const deadProxies: String[] = []
            for (const id of response.result as string[]) {
                const proxies = this.proxies.filter(p => p.id === id)
                if (proxies.length === 0) {
                    deadProxies.push(id)
                } else {
                    let found = false
                    for (const proxy of proxies) {
                        if (!proxy.proxy.deref()) {
                            const index = this.proxies.indexOf(proxy)
                            this.proxies.splice(index, 1)
                        } else {
                            found = true
                        }
                    }
                    if (!found) {
                        deadProxies.push(id)
                    }
                }
            }
            console.log('Cleaned up dead proxies', this.proxies)
            if (deadProxies.length > 0) {
                await this.command('cleanup', undefined, {ids: deadProxies}, 'cleanup')
            }
        } else {
            const callback = this.requestQueue.get(response.id)
            if (callback) {
                callback(response)
                this.requestQueue.delete(response.id)
            }
        }
    }

    async create<T>(type: string, constructor: ProxyConstructor<T>): Promise<T> {
        return this.command<T>(type, undefined, {}, 'create', constructor)
    }

    async invoke<T>(command: string, args: any, proxyConstructor?: ProxyConstructor<T>): Promise<T> {
        return this.command<T>(command, undefined, args, 'method', proxyConstructor)
    }

    async bind(instance: ProxyObject, event: string, callback: Function, constructors: ProxyConstructor<any>[]): Promise<ProxyEvent> {
        const id = await this.command<string>(event, instance, {}, 'event', undefined, 'add')
        const eventObject: ProxyEvent = {id, name: event, callback, paramConstructors: constructors}
        this.handlers.push(eventObject)
        return eventObject
    }

    async unbind(instance: ProxyObject, event: ProxyEvent): Promise<void> {
        await this.command(event.name, instance, {id: event.id}, 'event', undefined, 'remove')
        this.handlers = this.handlers.filter(e => e.id !== event.id)
    }

    async command<T>(command: string, instance: ProxyObject | undefined, args: any, type: InvocationType, proxyConstructor?: ProxyConstructor<T>, action?: string): Promise<T> {
        const request: InvocationRequest = {
            id: this.generateRequestId(),
            instanceId: instance?.id,
            type: type,
            name: command,
            action: action ?? '',
            arguments: args
        }
        this.ws.send(JSON.stringify(request))
        return new Promise<T>((resolve, reject) => {
            this.requestQueue.set(request.id, (response: InvocationResponse) => {
                if (response.error) {
                    reject(response.error)
                } else {
                    if (response.isProxy) {
                        if (proxyConstructor) {
                            const proxy = proxyConstructor(response.result.id, this) as ProxyObject
                            this.proxies.push({id: proxy.id, proxy: new WeakRef<ProxyObject>(proxy)})
                            resolve(proxy as T)
                        } else {
                            throw new Error("Invalid proxy object")
                        }
                    } else {
                        resolve(response.result)
                    }
                }
            })
        })
    }

    private generateRequestId(): string {
        return Math.random().toString(36).substring(2, 9);
    }
}
