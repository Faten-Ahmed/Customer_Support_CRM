import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { KbArticleEditorComponent } from './kb-article-editor.component';
import { KbService, KbArticle } from '../services/kb.service';
import { MatSnackBar } from '@angular/material/snack-bar';

const draftArticle: KbArticle = {
  id: 'art-new', title: 'T', content: 'C', categoryId: 'cat-1',
  visibility: 'Public', status: 'Draft', createdAt: '',
};

const mockCategories = [{ id: 'cat-1', name: 'General' }];

describe('KbArticleEditorComponent', () => {
  let fixture: ComponentFixture<KbArticleEditorComponent>;
  let component: KbArticleEditorComponent;
  const mockKbService = {
    listCategories: vi.fn().mockReturnValue(of(mockCategories)),
    create: vi.fn().mockReturnValue(of(draftArticle)),
    update: vi.fn().mockReturnValue(of({ ...draftArticle, id: 'art-1' })),
    submitForReview: vi.fn().mockReturnValue(of({ ...draftArticle, status: 'PendingReview' })),
    getById: vi.fn().mockReturnValue(of(draftArticle)),
  };
  const mockSnackBar = { open: vi.fn() };
  const mockRouter = { navigate: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.listCategories.mockReturnValue(of(mockCategories));
    mockKbService.create.mockReturnValue(of(draftArticle));
    mockKbService.update.mockReturnValue(of({ ...draftArticle, id: 'art-1' }));
    mockKbService.submitForReview.mockReturnValue(of({ ...draftArticle, status: 'PendingReview' }));

    await TestBed.configureTestingModule({
      imports: [KbArticleEditorComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: KbService, useValue: mockKbService },
        { provide: ActivatedRoute, useValue: { params: of({}) } },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(KbArticleEditorComponent);
    component = fixture.componentInstance;
    (component as any).router = mockRouter;
    fixture.detectChanges();
  });

  it('should create in new mode', () => {
    expect(component).toBeTruthy();
    expect(component.isEditMode).toBe(false);
    expect(component.categories().length).toBe(1);
  });

  it('should call create() on saveDraft when no articleId', () => {
    component.form.patchValue({
      title: 'My Article', categoryId: 'cat-1', content: 'Content here', visibility: 'Public',
    });
    component.saveDraft();
    expect(mockKbService.create).toHaveBeenCalled();
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/kb/articles', 'art-new', 'edit']);
  });

  it('should call update() then submitForReview() when articleId exists', () => {
    component.articleId = 'art-1';
    component.form.patchValue({
      title: 'My Article', categoryId: 'cat-1', content: 'Content here', visibility: 'Public',
    });
    component.submitForReview();
    expect(mockKbService.update).toHaveBeenCalled();
    expect(mockKbService.submitForReview).toHaveBeenCalledWith('art-1');
    expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/kb']);
  });
});
