import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { NotFoundComponent } from './not-found.component';

describe('NotFoundComponent', () => {
  let fixture: ComponentFixture<NotFoundComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotFoundComponent],
      providers: [provideRouter([])],
    }).compileComponents();
    fixture = TestBed.createComponent(NotFoundComponent);
    fixture.detectChanges();
  });

  it('should create', () => expect(fixture.componentInstance).toBeTruthy());

  it('should display 404 message', () => {
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('404');
  });

  it('should have a link back to home', () => {
    const link = (fixture.nativeElement as HTMLElement).querySelector('a');
    expect(link).toBeTruthy();
  });
});
