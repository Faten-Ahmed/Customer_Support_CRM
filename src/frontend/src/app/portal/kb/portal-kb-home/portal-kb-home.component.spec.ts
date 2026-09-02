import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalKbHomeComponent } from './portal-kb-home.component';
import { PortalKbService, PortalKbCategory, PortalKbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

const mockCategories: PortalKbCategory[] = [
  { id: 'c1', name: 'Billing' },
];

const mockArticles: PortalKbArticleSummary[] = [
  {
    id: 'a1',
    title: 'How to pay',
    titleAr: 'كيفية الدفع',
    categoryId: 'c1',
    categoryName: 'Billing',
    visibility: 'Public',
    createdAt: '2025-01-01T00:00:00Z',
  },
];

describe('PortalKbHomeComponent', () => {
  let fixture: ComponentFixture<PortalKbHomeComponent>;
  let component: PortalKbHomeComponent;
  const mockKbService = {
    getCategories: vi.fn().mockReturnValue(of(mockCategories)),
    list: vi.fn().mockReturnValue(of({ items: mockArticles, totalCount: 1 })),
  };
  const mockI18n = { lang: vi.fn().mockReturnValue('en'), isRtl: false };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.getCategories.mockReturnValue(of(mockCategories));
    mockKbService.list.mockReturnValue(of({ items: mockArticles, totalCount: 1 }));
    mockI18n.lang.mockReturnValue('en');

    await TestBed.configureTestingModule({
      imports: [PortalKbHomeComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PortalKbService, useValue: mockKbService },
        { provide: I18nService, useValue: mockI18n },
        { provide: ActivatedRoute, useValue: { queryParams: of({}) } },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load categories and recent articles on init', () => {
    expect(mockKbService.getCategories).toHaveBeenCalled();
    expect(mockKbService.list).toHaveBeenCalledWith({ pageSize: 6 });
    expect(component.categories().length).toBe(1);
    expect(component.articles().length).toBe(1);
  });

  it('should display category name', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Billing');
  });

  it('should return titleAr when lang is ar', () => {
    mockI18n.lang.mockReturnValue('ar');
    expect(component.articleTitle(mockArticles[0])).toBe('كيفية الدفع');
  });

  it('should return title when lang is en', () => {
    mockI18n.lang.mockReturnValue('en');
    expect(component.articleTitle(mockArticles[0])).toBe('How to pay');
  });

  it('should set searchControl value', () => {
    component.searchControl.setValue('password');
    expect(component.searchControl.value).toBe('password');
  });
});
