import { TestBed } from '@angular/core/testing';
import { SignalRService } from './signalr.service';
import { AuthStore } from '../../auth/auth.store';

describe('SignalRService', () => {
  let service: SignalRService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [SignalRService] });
    service = TestBed.inject(SignalRService);
  });

  it('should be created', () => expect(service).toBeTruthy());

  it('getConnection() should return a HubConnection for a given url', () => {
    const conn = service.getConnection('http://localhost/hubs/tickets');
    expect(conn).toBeTruthy();
    expect(typeof conn.on).toBe('function');
  });

  it('getConnection() should return the same connection on repeated calls', () => {
    const conn1 = service.getConnection('http://localhost/hubs/tickets');
    const conn2 = service.getConnection('http://localhost/hubs/tickets');
    expect(conn1).toBe(conn2);
  });
});
