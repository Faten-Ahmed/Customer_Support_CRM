import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { KbArticleListComponent } from './kb-article-list.component';
import { KbService, KbArticle } from '../services/kb.service';

const mockArticles: KbArticle[] = [
  { id: 'a1', title: 'Reset Password', content: '...', visibility: 'Public', status: 'Published', createdAt: '2025-01-01' },
  { id: 'a2', title: 'Billing FAQ', content: '...', visibility: 'Internal', status: 'Draft', createdAt: '2025-01-02' },
];

describe('KbArticleListComponent', () => {
  let fixture: ComponentFixture<KbArticleListComponent>;
  let component: KbArticleListComponent;
  const mockKbService = { list: vi.fn().mockReturnValue(of({ data: mockArticles, total: 2 })) };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.list.mockReturnValue(of({ data: mockArticles, total: 2 }));

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

  it('should create and load articles', () => {
    expect(component).toBeTruthy();
    expect(component.articles().length).toBe(2);
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
});
