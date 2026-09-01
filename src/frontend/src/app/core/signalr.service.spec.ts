import { TestBed } from '@angular/core/testing';
import { SignalRService, HubName, ConnectionState } from './signalr.service';
import { vi } from 'vitest';

const makeHubStub = () => {
  const handlers: Record<string, ((...args: any[]) => void)[]> = {};
  return {
    start: vi.fn().mockResolvedValue(undefined),
    stop: vi.fn().mockResolvedValue(undefined),
    on: vi.fn().mockImplementation((event: string, cb: (...a: any[]) => void) => {
      handlers[event] = handlers[event] ?? [];
      handlers[event].push(cb);
    }),
    onreconnecting: vi.fn(),
    onreconnected: vi.fn(),
    onclose: vi.fn(),
    state: 'Disconnected',
    _handlers: handlers,
    _emit(event: string, ...args: any[]) {
      (handlers[event] ?? []).forEach(cb => cb(...args));
    },
  };
};

describe('SignalRService', () => {
  let service: SignalRService;
  let hubStub: ReturnType<typeof makeHubStub>;

  beforeEach(() => {
    hubStub = makeHubStub();
    TestBed.configureTestingModule({
      providers: [SignalRService],
    });
    service = TestBed.inject(SignalRService);
    service['_createConnection'] = (_url: string) => hubStub as any;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('connect() should call start() on the hub', async () => {
    service.connect(HubName.Notification);
    await hubStub.start;
    expect(hubStub.start).toHaveBeenCalled();
  });

  it('connect() should set connectionState to Connected after start resolves', async () => {
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(service.connectionState(HubName.Notification)).toBe(ConnectionState.Connected);
  });

  it('disconnect() should call stop() on the hub', async () => {
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    service.disconnect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(hubStub.stop).toHaveBeenCalled();
  });

  it('notification$ should emit when ReceiveNotification fires', async () => {
    const payload = { id: 'n1', type: 'TicketAssigned', title: 'T', body: 'B' };
    let received: any = null;
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    service.notification$.subscribe(n => (received = n));
    hubStub._emit('ReceiveNotification', payload);
    expect(received).toEqual(payload);
  });

  it('unreadCountUpdated$ should emit when UnreadCountUpdated fires', async () => {
    let received: number | null = null;
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    service.unreadCountUpdated$.subscribe(c => (received = c));
    hubStub._emit('UnreadCountUpdated', 7);
    expect(received).toBe(7);
  });

  it('connectAll() should connect all three hubs', async () => {
    const stubs: ReturnType<typeof makeHubStub>[] = [];
    service['_createConnection'] = (_url: string) => {
      const s = makeHubStub();
      stubs.push(s);
      return s as any;
    };
    service.connectAll();
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(stubs.length).toBe(3);
    stubs.forEach(s => expect(s.start).toHaveBeenCalled());
  });

  it('should set connectionState to Reconnecting on onreconnecting callback', async () => {
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    const reconnectingCb = hubStub.onreconnecting.mock.calls[0][0];
    reconnectingCb();
    expect(service.connectionState(HubName.Notification)).toBe(ConnectionState.Reconnecting);
  });

  it('should set connectionState to Connected on onreconnected callback', async () => {
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    const reconnectingCb = hubStub.onreconnecting.mock.calls[0][0];
    reconnectingCb();
    const reconnectedCb = hubStub.onreconnected.mock.calls[0][0];
    reconnectedCb();
    expect(service.connectionState(HubName.Notification)).toBe(ConnectionState.Connected);
  });

  it('overallConnected() should be false when no hubs connected', () => {
    expect(service.overallConnected()).toBe(false);
  });

  it('overallConnected() should be true after NotificationHub connects', async () => {
    service.connect(HubName.Notification);
    await new Promise(resolve => setTimeout(resolve, 0));
    expect(service.overallConnected()).toBe(true);
  });
});
