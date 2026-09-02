import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { PortalProfileService } from './portal-profile.service';

describe('PortalProfileService', () => {
  let service: PortalProfileService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        PortalProfileService,
      ],
    });
    service = TestBed.inject(PortalProfileService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('get() GETs /api/v1/portal/profile', () => {
    service.get().subscribe();
    const req = http.expectOne('/api/v1/portal/profile');
    expect(req.request.method).toBe('GET');
    req.flush({ data: { id: 'c1', fullName: 'Jane Doe', fullNameAr: 'جين دو', email: 'jane@example.com' } });
  });

  it('update() PATCHes /api/v1/portal/profile', () => {
    service.update({ fullName: 'Jane Smith', phone: '555-1234' }).subscribe();
    const req = http.expectOne('/api/v1/portal/profile');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body.fullName).toBe('Jane Smith');
    req.flush({ data: { id: 'c1', fullName: 'Jane Smith', fullNameAr: 'جين سميث', email: 'jane@example.com', phone: '555-1234' } });
  });
});
