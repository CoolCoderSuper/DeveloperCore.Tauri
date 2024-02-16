import { type ProxyEvent, ProxyObject } from './proxyObject';
import { TauriConnector } from './tauriConnector';

export class TestInput extends ProxyObject {
    static create(id: string, connector: TauriConnector): TestInput {
        return new TestInput(id, connector)
    }

    get name(): Promise<string> {
        return this.get<string>("Name")
    }

    async setName(value: string) {
        await this.set("Name", value)
    }

    get child(): Promise<TestInput> {
        return this.get<TestInput>("Child", TestInput.create)
    }

    async setChild(value: TestInput) {
        await this.set("Child", value)
    }

    async toString(): Promise<string> {
        return this.invoke<string>("ToString", {})
    }

    async updateName(name: string): Promise<TestInput> {
        return this.invoke<TestInput>("UpdateName", {name}, TestInput.create)
    }

    async raiseTestEvent(): Promise<void> {
        return this.invoke<void>("RaiseTestEvent", {})
    }

    async bindTestEvent(callback: (sender: TestInput, e: any) => void): Promise<ProxyEvent> {
        return this.bind("TestEvent", callback, [TestInput.create])
    }

    async unbindTestEvent(event: ProxyEvent): Promise<void> {
        return this.unbind(event)
    }

    async raiseTestEvent2(): Promise<void> {
        return this.invoke<void>("RaiseTestEvent2", {})
    }

    async bindTestEvent2(callback: (sender: TestInput, e: any) => void): Promise<ProxyEvent> {
        return this.bind("TestEvent2", callback, [TestInput.create])
    }

    async unbindTestEvent2(event: ProxyEvent): Promise<void> {
        return this.unbind(event)
    }
}
