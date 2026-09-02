import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { KbArticleDetailComponent } from './kb-article-detail.component';
import { KbService, KbArticle } from '../services/kb.service';
import { AuthStore } from '../../auth/auth.store';

const pendingArticle: KbArticle = {
  id: 'art-1',
  title: 'How to reset password',
  content: '# Reset\n\nFollow these steps.',
  visibility: 'Public',
  status: 'PendingReview',
  createdAt: '2025-01-01',
};

const publishedArticle: KbArticle = { ...pendingArticle, status: 'Published' };
const archivedArticle: KbArticle = { ...pendingArticle, status: 'Archived' };

describe('KbArticleDetailComponent', () => {
  let fixture: ComponentFixture<KbArticleDetailComponent>;
  let component: KbArticleDetailComponent;
  const mockRouter = { navigate: vi.fn() };
  const mockKbService = {
    getById: vi.fn().mockReturnValue(of(pendingArticle)),
    approve: vi.fn().mockReturnValue(of({ ...pendingArticle, status: 'Published' })),
    reject: vi.fn().mockReturnValue(of({ ...pendingArticle, status: 'Draft' })),
    archive: vi.fn().mockReturnValue(of(undefined)),
  };
  const mockDialog = { open: vi.fn() };
  const mockSnackBar = { open: vi.fn() };
  const mockAuthStore = { user: vi.fn().mockReturnValue({ role: 'Manager' }) };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.getById.mockReturnValue(of(pendingArticle));
    mockKbService.approve.mockReturnValue(of({ ...pendingArticle, status: 'Published' }));
    mockKbService.archive.mockReturnValue(of(undefined));
    mockAuthStore.user.mockReturnValue({ role: 'Manager' });

    await TestBed.configureTestingModule({
      imports: [KbArticleDetailComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: KbService, useValue: mockKbService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: ActivatedRoute, useValue: { params: of({ id: 'art-1' }) } },
        { provide: AuthStore, useValue: mockAuthStore },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(KbArticleDetailComponent);
    component = fixture.componentInstance;
    (component as any).router = mockRouter;
    fixture.detectChanges();
  });

  it('should load the article', () => {
    expect(component.article()?.title).toBe('How to reset password');
  });

  it('should show Approve and Reject buttons for Manager on PendingReview article', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Approve');
    expect(el.textContent).toContain('Reject');
  });

  it('should call approve() and navigate to /app/kb', () => {
    component.approve();
    expect(mockKbService.approve).toHaveBeenCalledWith('art-1');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/kb']);
  });

  it('should return canReview=false for Agent role', () => {
    (component as any).authStore = { user: () => ({ role: 'Agent' }) };
    expect(component.canReview).toBe(false);
  });

  it('canArchive should be true for Manager on non-Archived article', () => {
    expect(component.canArchive).toBe(true);
  });

  it('canArchive should be false when article is already Archived', () => {
    mockKbService.getById.mockReturnValue(of(archivedArticle));
    component.ngOnInit();
    fixture.detectChanges();
    expect(component.canArchive).toBe(false);
  });

  it('canArchive should be false for Agent role', () => {
    (component as any).authStore = { user: () => ({ role: 'Agent' }) };
    expect(component.canArchive).toBe(false);
  });

  it('should call archive() and navigate to /app/kb', () => {
    component.archive();
    expect(mockKbService.archive).toHaveBeenCalledWith('art-1');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/kb']);
  });

  it('should show Archive button for Manager on Published article', () => {
    mockKbService.getById.mockReturnValue(of(publishedArticle));
    component.ngOnInit();
    fixture.detectChanges();
    expect(component.canArchive).toBe(true);
  });
});
