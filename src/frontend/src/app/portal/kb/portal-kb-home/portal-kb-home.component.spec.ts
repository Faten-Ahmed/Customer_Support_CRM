import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalKbHomeComponent } from './portal-kb-home.component';
import { PortalKbService, KbCategory, KbArticleSummary } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

const mockCategories: KbCategory[] = [
  { id: 'c1', name: { en: 'Billing', ar: 'الفواتير' }, articleCount: 5 },
];

const mockFeatured: KbArticleSummary[] = [
  {
    id: 'a1',
    title: { en: 'How to pay', ar: 'كيفية الدفع' },
    excerpt: { en: 'Details', ar: 'تفاصيل' },
    categoryId: 'c1',
    categoryName: { en: 'Billing', ar: 'الفواتير' },
    featured: true,
    updatedAt: '',
  },
];

describe('PortalKbHomeComponent', () => {
  let fixture: ComponentFixture<PortalKbHomeComponent>;
  let component: PortalKbHomeComponent;
  const mockKbService = {
    getCategories: vi.fn().mockReturnValue(of(mockCategories)),
    list: vi.fn().mockReturnValue(of(mockFeatured)),
  };
  const mockI18n = { lang: vi.fn().mockReturnValue('en'), isRtl: false };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.getCategories.mockReturnValue(of(mockCategories));
    mockKbService.list.mockReturnValue(of(mockFeatured));
    mockI18n.lang.mockReturnValue('en');

    await TestBed.configureTestingModule({
      imports: [PortalKbHomeComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PortalKbService, useValue: mockKbService },
        { provide: I18nService, useValue: mockI18n },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbHomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load categories and featured articles on init', () => {
    expect(mockKbService.getCategories).toHaveBeenCalled();
    expect(mockKbService.list).toHaveBeenCalledWith({ featured: true });
    expect(component.categories().length).toBe(1);
    expect(component.featured().length).toBe(1);
  });

  it('should display category name in current language', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Billing');
  });

  it('should set searchControl value', () => {
    component.searchControl.setValue('password');
    expect(component.searchControl.value).toBe('password');
  });
});
