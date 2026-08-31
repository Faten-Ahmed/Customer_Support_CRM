import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalKbArticleComponent } from './portal-kb-article.component';
import { PortalKbService, PortalKbArticle } from '../../services/portal-kb.service';
import { I18nService } from '../../../shared/services/i18n.service';

const mockArticle: PortalKbArticle = {
  id: 'a1',
  title: 'Reset Password',
  titleAr: 'إعادة تعيين كلمة المرور',
  content: 'Step 1: Go to login page',
  contentAr: 'الخطوة 1: اذهب إلى صفحة تسجيل الدخول',
  categoryId: 'c1',
  categoryName: 'Account',
  visibility: 'Public',
  status: 'Published',
  publishedAt: '2025-01-01T00:00:00Z',
  createdAt: '2025-01-01T00:00:00Z',
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

  it('should render article title in English', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Reset Password');
  });

  it('should return Arabic title when lang is ar', () => {
    mockI18n.lang.mockReturnValue('ar');
    expect(component.articleTitle(mockArticle)).toBe('إعادة تعيين كلمة المرور');
  });

  it('should return Arabic content when lang is ar', () => {
    mockI18n.lang.mockReturnValue('ar');
    expect(component.articleContent(mockArticle)).toBe('الخطوة 1: اذهب إلى صفحة تسجيل الدخول');
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
