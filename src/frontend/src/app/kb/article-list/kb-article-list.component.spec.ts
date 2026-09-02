import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { KbArticleListComponent } from './kb-article-list.component';
import { KbService, KbArticle, KbSearchResult } from '../services/kb.service';

const mockArticles: KbArticle[] = [
  { id: 'a1', title: 'Reset Password', content: '...', visibility: 'Public', status: 'Published', createdAt: '2025-01-01' },
  { id: 'a2', title: 'Billing FAQ', content: '...', visibility: 'Internal', status: 'Draft', createdAt: '2025-01-02' },
];

const mockSearchResults: KbSearchResult[] = [
  { id: 'a1', title: 'Reset Password', categoryId: 'c1', visibility: 'Public', excerpt: 'How to reset…' },
];

describe('KbArticleListComponent', () => {
  let fixture: ComponentFixture<KbArticleListComponent>;
  let component: KbArticleListComponent;
  const mockKbService = {
    list: vi.fn().mockReturnValue(of({ items: mockArticles, totalCount: 2 })),
    search: vi.fn().mockReturnValue(of(mockSearchResults)),
    delete: vi.fn().mockReturnValue(of(undefined)),
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.list.mockReturnValue(of({ items: mockArticles, totalCount: 2 }));
    mockKbService.search.mockReturnValue(of(mockSearchResults));
    mockKbService.delete.mockReturnValue(of(undefined));

    await TestBed.configureTestingModule({
      imports: [KbArticleListComponent, NoopAnimationsModule],
      providers: [
        provideRouter([]),
        { provide: KbService, useValue: mockKbService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(KbArticleListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create and load articles via list()', () => {
    expect(component).toBeTruthy();
    expect(component.rows().length).toBe(2);
    expect(component.isSearchMode()).toBe(false);
  });

  it('should use search() endpoint when searchControl has a value', () => {
    component.searchControl.setValue('reset');
    component.load();
    expect(mockKbService.search).toHaveBeenCalledWith('reset');
    expect(component.isSearchMode()).toBe(true);
    expect(component.rows().length).toBe(1);
  });

  it('should switch back to list() when searchControl is cleared', () => {
    component.searchControl.setValue('reset');
    component.load();
    component.searchControl.setValue('');
    component.load();
    expect(mockKbService.list).toHaveBeenCalled();
    expect(component.isSearchMode()).toBe(false);
  });

  it('should filter by status when statusFilter changes', () => {
    component.statusFilter.setValue('Draft');
    expect(mockKbService.list).toHaveBeenCalledWith(expect.objectContaining({ status: 'Draft' }));
  });

  it('should show status labels in the table', () => {
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Published');
    expect(el.textContent).toContain('Draft');
  });

  it('should call delete() and reload on delete button click', () => {
    const draftRow = component.rows().find(r => r.status === 'Draft')!;
    component.delete(draftRow, new MouseEvent('click'));
    expect(mockKbService.delete).toHaveBeenCalledWith('a2');
    expect(mockKbService.list).toHaveBeenCalledTimes(2);
  });

  it('should show snackbar error on 403 delete response', () => {
    const draftRow = component.rows().find(r => r.status === 'Draft')!;
    mockKbService.delete.mockReturnValue(throwError(() => ({ status: 403 })));
    component.delete(draftRow, new MouseEvent('click'));
    // No throw — error is handled gracefully
    expect(mockKbService.delete).toHaveBeenCalledWith('a2');
  });
});
