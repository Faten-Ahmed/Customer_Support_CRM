import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialog } from '@angular/material/dialog';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { CategoryTreeComponent } from './category-tree.component';
import { CategoryService, Category } from './category.service';

const mockCategories: Category[] = [
  { id: 'cat1', name: 'Hardware', sortOrder: 1, isActive: true },
  { id: 'cat2', name: 'Networking', sortOrder: 2, isActive: true, parentCategoryId: 'cat1' },
  { id: 'cat3', name: 'Software', sortOrder: 3, isActive: false },
];

describe('CategoryTreeComponent', () => {
  let fixture: ComponentFixture<CategoryTreeComponent>;
  let component: CategoryTreeComponent;
  let categoryService: {
    list: ReturnType<typeof vi.fn>;
    deactivate: ReturnType<typeof vi.fn>;
    reactivate: ReturnType<typeof vi.fn>;
  };

  beforeEach(async () => {
    categoryService = {
      list: vi.fn().mockReturnValue(of({ data: mockCategories })),
      deactivate: vi.fn().mockReturnValue(of({})),
      reactivate: vi.fn().mockReturnValue(of({})),
    };

    await TestBed.configureTestingModule({
      imports: [CategoryTreeComponent, NoopAnimationsModule],
      providers: [
        { provide: CategoryService, useValue: categoryService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(CategoryTreeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load categories on init', () => {
    expect(categoryService.list).toHaveBeenCalled();
  });

  it('should build tree with root categories', () => {
    // cat1 and cat3 are roots; cat2 is child of cat1
    const roots = component.categories();
    expect(roots.length).toBe(2);
    const hardware = roots.find(c => c.id === 'cat1');
    expect(hardware).toBeTruthy();
    expect(hardware!.children?.length).toBe(1);
    expect(hardware!.children![0].id).toBe('cat2');
  });

  it('should open dialog for new category', () => {
    const dialog = fixture.debugElement.injector.get(MatDialog);
    vi.spyOn(dialog, 'open').mockReturnValue({ afterClosed: () => of(null) } as any);
    component.openNewCategoryDialog();
    expect(dialog.open).toHaveBeenCalled();
  });

  it('should deactivate a category', () => {
    component.deactivate(mockCategories[0]);
    expect(categoryService.deactivate).toHaveBeenCalledWith('cat1');
  });
});
