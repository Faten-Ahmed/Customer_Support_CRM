import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PortalProfileComponent } from './portal-profile.component';
import { PortalProfileService, PortalProfile } from '../services/portal-profile.service';

const mockProfile: PortalProfile = {
  id: 'c1',
  fullName: 'Jane Doe',
  fullNameAr: 'جين دو',
  email: 'jane@example.com',
  phone: '555-0000',
  city: 'Riyadh',
  companyName: 'ACME Corp',
};

describe('PortalProfileComponent', () => {
  let fixture: ComponentFixture<PortalProfileComponent>;
  let component: PortalProfileComponent;
  let profileService: { get: ReturnType<typeof vi.fn>; update: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    profileService = {
      get: vi.fn().mockReturnValue(of({ data: mockProfile })),
      update: vi.fn().mockReturnValue(of({ data: { ...mockProfile, fullName: 'Jane Smith' } })),
    };

    await TestBed.configureTestingModule({
      imports: [PortalProfileComponent, ReactiveFormsModule, NoopAnimationsModule],
      providers: [{ provide: PortalProfileService, useValue: profileService }],
    }).compileComponents();

    fixture = TestBed.createComponent(PortalProfileComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
    await fixture.whenStable();
  });

  it('should create', () => expect(component).toBeTruthy());

  it('should load profile on init', () => {
    expect(profileService.get).toHaveBeenCalled();
    expect(component.profile()).toEqual(mockProfile);
  });

  it('should populate form with profile values', () => {
    expect(component.form.get('fullName')!.value).toBe('Jane Doe');
  });

  it('email field should always be disabled', () => {
    expect(component.form.get('email')!.disabled).toBe(true);
  });

  it('form fields are disabled in view mode', () => {
    expect(component.editMode()).toBe(false);
    expect(component.form.get('fullName')!.disabled).toBe(true);
  });

  it('should enable editable fields when entering edit mode', () => {
    component.enterEditMode();
    expect(component.editMode()).toBe(true);
    expect(component.form.get('fullName')!.disabled).toBe(false);
    expect(component.form.get('phone')!.disabled).toBe(false);
  });

  it('should PATCH profile on save and exit edit mode', async () => {
    component.enterEditMode();
    component.form.get('fullName')!.setValue('Jane Smith');
    component.save();
    await fixture.whenStable();
    expect(profileService.update).toHaveBeenCalledWith(
      expect.objectContaining({ fullName: 'Jane Smith' })
    );
    expect(component.editMode()).toBe(false);
  });

  it('should cancel edit and restore original values', () => {
    component.enterEditMode();
    component.form.get('fullName')!.setValue('Changed Name');
    component.cancelEdit();
    expect(component.editMode()).toBe(false);
    expect(component.form.get('fullName')!.value).toBe('Jane Doe');
  });
});
