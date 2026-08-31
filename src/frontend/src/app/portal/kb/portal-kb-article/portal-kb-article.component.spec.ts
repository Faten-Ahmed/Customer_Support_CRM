import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalKbArticleComponent } from './portal-kb-article.component';
import { PortalKbService, KbArticle } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

const mockArticle: KbArticle = {
  id: 'a1',
  title: { en: 'Reset Password', ar: 'إعادة تعيين كلمة المرور' },
  excerpt: { en: 'Steps', ar: 'خطوات' },
  content: { en: '<p>Step 1: Go to login page</p>', ar: '<p>الخطوة 1</p>' },
  categoryId: 'c1',
  categoryName: { en: 'Account', ar: 'الحساب' },
  featured: false,
  updatedAt: '2025-01-01T00:00:00Z',
};

describe('PortalKbArticleComponent', () => {
  let fixture: ComponentFixture<PortalKbArticleComponent>;
  let component: PortalKbArticleComponent;
  const mockKbService = { getById: vi.fn().mockReturnValue(of(mockArticle)) };
  const mockI18n = { lang: vi.fn().mockReturnValue('en'), isRtl: false };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.getById.mockReturnValue(of(mockArticle));
    mockI18n.lang.mockReturnValue('en');

    await TestBed.configureTestingModule({
      imports: [PortalKbArticleComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: PortalKbService, useValue: mockKbService },
        { provide: I18nService, useValue: mockI18n },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: { get: () => 'a1' } } },
        },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalKbArticleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load article by id', () => {
    expect(mockKbService.getById).toHaveBeenCalledWith('a1');
    expect(component.article()).toBeTruthy();
  });

  it('should render article title in current language', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Reset Password');
  });

  it('should record thumbsUp feedback', () => {
    component.submitFeedback(true);
    expect(component.feedbackGiven()).toBe('up');
  });

  it('should record thumbsDown feedback', () => {
    component.submitFeedback(false);
    expect(component.feedbackGiven()).toBe('down');
  });
});
