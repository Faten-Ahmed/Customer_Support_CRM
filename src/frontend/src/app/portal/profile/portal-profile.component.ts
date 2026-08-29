import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatCardModule } from '@angular/material/card';
import { PortalProfileService, PortalProfile } from '../services/portal-profile.service';

@Component({
  selector: 'app-portal-profile',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule,
    MatFormFieldModule, MatInputModule, MatButtonModule,
    MatIconModule, MatTooltipModule, MatProgressSpinnerModule, MatCardModule,
  ],
  template: `
    <div class="profile-wrap">
      <mat-card>
        <mat-card-content>
          <div class="profile-header">
            <h1>My Profile</h1>
            @if (!editMode()) {
              <button mat-icon-button (click)="enterEditMode()" matTooltip="Edit profile" aria-label="Edit profile">
                <mat-icon>edit</mat-icon>
              </button>
            }
          </div>

          @if (loading()) {
            <div class="center"><mat-spinner diameter="40" /></div>
          } @else if (profile()) {
            <form [formGroup]="form" class="profile-form">

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Full Name</mat-label>
                <input matInput formControlName="fullName" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>الاسم الكامل (Arabic)</mat-label>
                <input matInput formControlName="fullNameAr" dir="rtl" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input matInput formControlName="email" />
                <mat-icon matSuffix
                  matTooltip="Email cannot be changed here. Contact support to update your email.">
                  lock
                </mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Phone</mat-label>
                <input matInput formControlName="phone" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>City</mat-label>
                <input matInput formControlName="city" />
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Company</mat-label>
                <input matInput formControlName="companyName" />
              </mat-form-field>

              @if (editMode()) {
                <div class="form-actions">
                  <button mat-stroked-button type="button" (click)="cancelEdit()">Cancel</button>
                  <button mat-flat-button color="primary" type="button"
                          (click)="save()" [disabled]="saving()">
                    @if (saving()) { <mat-spinner diameter="18" /> } @else { Save Changes }
                  </button>
                </div>
              }
            </form>
          }
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .profile-wrap { max-width: 560px; margin: 24px auto; padding: 0 16px; }
    .profile-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 20px; }
    h1 { margin: 0; font-size: 20px; font-weight: 600; }
    .profile-form { display: flex; flex-direction: column; gap: 4px; }
    .full-width { width: 100%; }
    .center { display: flex; justify-content: center; padding: 32px; }
    .form-actions { display: flex; justify-content: flex-end; gap: 12px; margin-top: 8px; }
  `],
})
export class PortalProfileComponent implements OnInit {
  private readonly profileService = inject(PortalProfileService);
  private readonly fb = inject(FormBuilder);

  readonly profile = signal<PortalProfile | null>(null);
  readonly editMode = signal(false);
  readonly loading = signal(true);
  readonly saving = signal(false);

  form = this.fb.group({
    fullName: [{ value: '', disabled: true }],
    fullNameAr: [{ value: '', disabled: true }],
    email: [{ value: '', disabled: true }],
    phone: [{ value: '', disabled: true }],
    city: [{ value: '', disabled: true }],
    companyName: [{ value: '', disabled: true }],
  });

  ngOnInit(): void {
    this.profileService.get().subscribe({
      next: res => {
        const p = res.data;
        this.profile.set(p);
        this.form.patchValue({
          fullName: p.fullName,
          fullNameAr: p.fullNameAr,
          email: p.email,
          phone: p.phone ?? '',
          city: p.city ?? '',
          companyName: p.companyName ?? '',
        });
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  enterEditMode(): void {
    this.editMode.set(true);
    this.form.get('fullName')!.enable();
    this.form.get('fullNameAr')!.enable();
    this.form.get('phone')!.enable();
    this.form.get('city')!.enable();
  }

  cancelEdit(): void {
    const p = this.profile();
    if (p) {
      this.form.patchValue({
        fullName: p.fullName,
        fullNameAr: p.fullNameAr,
        phone: p.phone ?? '',
        city: p.city ?? '',
        companyName: p.companyName ?? '',
      });
    }
    this._disableEditableFields();
    this.editMode.set(false);
  }

  save(): void {
    this.saving.set(true);
    const val = this.form.getRawValue();
    this.profileService.update({
      fullName: val.fullName ?? undefined,
      fullNameAr: val.fullNameAr ?? undefined,
      phone: val.phone ?? undefined,
      city: val.city ?? undefined,
    }).subscribe({
      next: res => {
        this.profile.set(res.data);
        this._disableEditableFields();
        this.editMode.set(false);
        this.saving.set(false);
      },
      error: () => this.saving.set(false),
    });
  }

  private _disableEditableFields(): void {
    this.form.get('fullName')!.disable();
    this.form.get('fullNameAr')!.disable();
    this.form.get('phone')!.disable();
    this.form.get('city')!.disable();
  }
}
