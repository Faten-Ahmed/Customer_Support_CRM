import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { RejectDialogComponent } from './reject-dialog.component';
import { KbService, KbArticle } from '../services/kb.service';

const rejectedArticle: KbArticle = {
  id: 'art-1', title: 'T', content: 'C', visibility: 'Public', status: 'Draft', createdAt: '',
};

describe('RejectDialogComponent', () => {
  let fixture: ComponentFixture<RejectDialogComponent>;
  let component: RejectDialogComponent;
  const mockKbService = { reject: vi.fn().mockReturnValue(of(rejectedArticle)) };
  const mockDialogRef = { close: vi.fn() };
  const mockSnackBar = { open: vi.fn() };

  beforeEach(async () => {
    vi.clearAllMocks();
    mockKbService.reject.mockReturnValue(of(rejectedArticle));

    await TestBed.configureTestingModule({
      imports: [RejectDialogComponent, NoopAnimationsModule],
      providers: [
        { provide: KbService, useValue: mockKbService },
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: { articleId: 'art-1' } },
        { provide: MatSnackBar, useValue: mockSnackBar },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(RejectDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should disable submit when note is too short', () => {
    component.noteControl.setValue('short');
    expect(component.noteControl.invalid).toBe(true);
  });

  it('should call reject() and close dialog on valid note', () => {
    component.noteControl.setValue('This needs much more detail and context.');
    component.onReject();
    expect(mockKbService.reject).toHaveBeenCalledWith('art-1', 'This needs much more detail and context.');
    expect(mockDialogRef.close).toHaveBeenCalledWith(true);
  });
});
