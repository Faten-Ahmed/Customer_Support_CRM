import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { AbstractControl, FormBuilder, FormGroup, ReactiveFormsModule, ValidationErrors, ValidatorFn, Validators } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { MatTabsModule } from '@angular/material/tabs';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';
import { finalize } from 'rxjs/operators';
import { CustomerDetail, CustomerTicket, CustomerService } from '../services/customer.service';
import { AuthStore } from '../../auth/auth.store';

@Component({
  selector: 'app-customer-detail',
  standalone: true,
  imports: [
    ReactiveFormsModule, RouterModule, DatePipe,
    MatTabsModule, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatCheckboxModule,
    MatTableModule, MatPaginatorModule,
    MatSnackBarModule, MatProgressSpinnerModule, MatDividerModule, MatTooltipModule,
  ],
  templateUrl: './customer-detail.component.html',
  styleUrl: './customer-detail.component.scss',
})
export class CustomerDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly customerService = inject(CustomerService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly fb = inject(FormBuilder);
  readonly authStore = inject(AuthStore);

  readonly customer = signal<CustomerDetail | null>(null);
  readonly loading = signal(false);

  private static readonly PHONE_PATTERN: ValidatorFn = (control: AbstractControl): ValidationErrors | null => {
    const v = (control.value ?? '').toString().trim();
    if (!v) return null;
    return /^\+?[\d\s\-().]{7,20}$/.test(v) ? null : { phone: true };
  };

  // overview edit
  readonly editing = signal(false);
  editForm: FormGroup = this.fb.group({
    fullName: ['', Validators.required],
    phone: ['', CustomerDetailComponent.PHONE_PATTERN],
    companyName: [''],
  });
  readonly savingEdit = signal(false);

  // vip
  readonly vipLoading = signal(false);

  // contacts
  readonly contactForm: FormGroup = this.fb.group({
    type: ['Phone', Validators.required],
    value: ['', [Validators.required, CustomerDetailComponent.PHONE_PATTERN]],
    isPrimary: [false],
  });
  readonly contactSubmitting = signal(false);
  readonly removingContactId = signal<string | null>(null);
  readonly contactTypes = ['Phone', 'Email', 'WhatsApp'];

  // tickets
  readonly tickets = signal<CustomerTicket[]>([]);
  readonly ticketsTotal = signal(0);
  readonly ticketsLoading = signal(false);
  ticketsPage = 1;
  ticketsPageSize = 10;
  readonly ticketColumns = ['ticketNumber', 'subject', 'status', 'priority', 'category', 'createdAt'];

  ngOnInit(): void {
    this.route.params.subscribe(params => {
      this.loadCustomer(params['id']);
    });
    this.contactForm.get('type')!.valueChanges.subscribe(type => {
      this.updateContactValueValidators(type);
    });
  }

  private updateContactValueValidators(type: string): void {
    const valueCtrl = this.contactForm.get('value')!;
    valueCtrl.clearValidators();
    if (type === 'Email') {
      valueCtrl.setValidators([Validators.required, Validators.email]);
    } else {
      valueCtrl.setValidators([Validators.required, CustomerDetailComponent.PHONE_PATTERN]);
    }
    valueCtrl.updateValueAndValidity();
  }

  get contactValuePlaceholder(): string {
    const t = this.contactForm.get('type')?.value;
    if (t === 'Email') return 'name@example.com';
    if (t === 'WhatsApp') return '+966 5x xxx xxxx';
    return '+966 5x xxx xxxx';
  }

  private loadCustomer(id: string): void {
    this.loading.set(true);
    this.customerService.getById(id).subscribe({
      next: c => {
        this.customer.set(c);
        this.editForm.patchValue({ fullName: c.fullName, phone: c.phone ?? '', companyName: c.companyName ?? '' });
        this.loading.set(false);
        this.loadTickets(c.id);
      },
      error: () => this.loading.set(false),
    });
  }

  // ── Overview ──────────────────────────────────────────────────────────────

  enterEditMode(): void { this.editing.set(true); }

  cancelEdit(): void {
    const c = this.customer();
    if (c) this.editForm.patchValue({ fullName: c.fullName, phone: c.phone ?? '', companyName: c.companyName ?? '' });
    this.editing.set(false);
  }

  saveChanges(): void {
    const c = this.customer();
    if (!c || this.editForm.invalid) return;
    this.savingEdit.set(true);
    this.customerService.update(c.id, this.editForm.value).pipe(finalize(() => this.savingEdit.set(false))).subscribe({
      next: updated => {
        this.customer.set(updated);
        this.editing.set(false);
        this.snackBar.open('Customer updated', 'OK', { duration: 3000 });
      },
    });
  }

  // ── VIP ───────────────────────────────────────────────────────────────────

  toggleVip(): void {
    const c = this.customer();
    if (!c) return;
    this.vipLoading.set(true);
    this.customerService.setVip(c.id, !c.isVip).pipe(finalize(() => this.vipLoading.set(false))).subscribe({
      next: () => {
        this.customer.update(prev => prev ? { ...prev, isVip: !prev.isVip } : prev);
        this.snackBar.open(c.isVip ? 'VIP removed' : 'Marked as VIP', 'OK', { duration: 3000 });
      },
    });
  }

  // ── Contacts ──────────────────────────────────────────────────────────────

  addContact(): void {
    if (this.contactForm.invalid) { this.contactForm.markAllAsTouched(); return; }
    const c = this.customer();
    if (!c) return;
    this.contactSubmitting.set(true);
    this.customerService.addContact(c.id, this.contactForm.value).pipe(finalize(() => this.contactSubmitting.set(false))).subscribe({
      next: contact => {
        this.customer.update(prev => prev ? { ...prev, contacts: [...prev.contacts, contact] } : prev);
        this.contactForm.reset({ type: 'Phone', value: '', isPrimary: false });
        this.snackBar.open('Contact added', 'OK', { duration: 3000 });
      },
      error: (err) => {
        const msg = err?.error?.errors?.[0]?.message ?? 'Failed to add contact';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      },
    });
  }

  removeContact(contactId: string): void {
    const c = this.customer();
    if (!c) return;
    this.removingContactId.set(contactId);
    this.customerService.removeContact(c.id, contactId).pipe(finalize(() => this.removingContactId.set(null))).subscribe({
      next: () => {
        this.customer.update(prev => prev ? { ...prev, contacts: prev.contacts.filter(ct => ct.id !== contactId) } : prev);
        this.snackBar.open('Contact removed', 'OK', { duration: 3000 });
      },
      error: (err) => {
        const msg = err?.error?.errors?.[0]?.message ?? 'Failed to remove contact';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      },
    });
  }

  // ── Tickets ───────────────────────────────────────────────────────────────

  loadTickets(customerId: string): void {
    this.ticketsLoading.set(true);
    this.customerService.getTickets(customerId, this.ticketsPage, this.ticketsPageSize).pipe(finalize(() => this.ticketsLoading.set(false))).subscribe({
      next: res => {
        this.tickets.set(res.items);
        this.ticketsTotal.set(res.meta.totalCount);
      },
    });
  }

  onTicketsPage(event: PageEvent): void {
    const c = this.customer();
    if (!c) return;
    this.ticketsPage = event.pageIndex + 1;
    this.ticketsPageSize = event.pageSize;
    this.loadTickets(c.id);
  }

  // ── Deactivate / Reactivate ───────────────────────────────────────────────

  readonly deactivating = signal(false);
  readonly reactivating = signal(false);

  confirmDeactivate(): void {
    const c = this.customer();
    if (!c) return;
    this.deactivating.set(true);
    this.customerService.deactivate(c.id).pipe(finalize(() => this.deactivating.set(false))).subscribe({
      next: () => {
        this.customer.update(prev => prev ? { ...prev, isActive: false } : prev);
        this.snackBar.open('Customer deactivated', 'OK', { duration: 3000 });
      },
      error: (err) => {
        const msg = err?.error?.errors?.[0]?.message ?? 'Failed to deactivate customer';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      },
    });
  }

  confirmReactivate(): void {
    const c = this.customer();
    if (!c) return;
    this.reactivating.set(true);
    this.customerService.reactivate(c.id).pipe(finalize(() => this.reactivating.set(false))).subscribe({
      next: () => {
        this.customer.update(prev => prev ? { ...prev, isActive: true } : prev);
        this.snackBar.open('Customer reactivated', 'OK', { duration: 3000 });
      },
      error: (err) => {
        const msg = err?.error?.errors?.[0]?.message ?? 'Failed to reactivate customer';
        this.snackBar.open(msg, 'OK', { duration: 4000 });
      },
    });
  }

  // ── Role helpers ──────────────────────────────────────────────────────────

  get isAdmin(): boolean { return this.authStore.user()?.role === 'Admin'; }
  get isAdminOrManager(): boolean {
    const r = this.authStore.user()?.role;
    return r === 'Admin' || r === 'Manager';
  }
}
