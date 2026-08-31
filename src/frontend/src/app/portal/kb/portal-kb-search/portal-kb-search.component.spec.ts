import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalKbSearchComponent } from './portal-kb-search.component';
import { PortalKbService, PortalKbSearchResult } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

const mockResults: PortalKbSearchResult[] = [
  {
    id: 'a1',
    title: 'Reset Password',
    titleAr: 'إعادة تعيين كلمة المرور',
    categoryId: 'c1',
    visibility: 'Public',
    excerpt: 'Steps to reset your password.',
  },
];

describe('PortalKbSearchComponent', () => {
  let fixture: ComponentFixture<PortalKbSearchComponent>;
  let component: PortalKbSearchComponent;
  const mockKbService = {
    search: vi.fn().mockReturnValue(of(mockResults)),
  };
  const mockI18n = { lang: vi.fn().mockReturnValue('en'), isRtl: false };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.search.mockReturnValue(of(mockResults));
    mockI18n.lang.mockReturnValue('en');

    await TestBed.configureTestingModule({
      imports: [PortalKbSearchComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PortalKbService, useValue: mockKbService },
        { provide: I18nService, useValue: mockI18n },
        { provide: ActivatedRoute, useValue: { queryParams: of({ q: 'password' }) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should call search() with q from queryParams', () => {
    expect(mockKbService.search).toHaveBeenCalledWith('password');
    expect(component.results().length).toBe(1);
  });

  it('should display search results', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Reset Password');
  });

  it('should return Arabic title when lang is ar', () => {
    mockI18n.lang.mockReturnValue('ar');
    expect(component.articleTitle(mockResults[0])).toBe('إعادة تعيين كلمة المرور');
  });

  it('should show empty state when no results', () => {
    mockKbService.search.mockReturnValue(of([]));
    component.runSearch('nothing');
    fixture.detectChanges();
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('No articles found');
  });
});
