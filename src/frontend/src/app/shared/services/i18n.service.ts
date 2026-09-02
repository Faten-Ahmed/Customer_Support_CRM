import { Injectable, signal, computed } from '@angular/core';

export type AppLang = 'en' | 'ar';

@Injectable({ providedIn: 'root' })
export class I18nService {
  private readonly _lang = signal<AppLang>('en');

  readonly lang = this._lang.asReadonly();
  readonly dir = computed<'ltr' | 'rtl'>(() => this._lang() === 'ar' ? 'rtl' : 'ltr');
  readonly isRtl = computed(() => this._lang() === 'ar');

  init(): void {
    const saved = localStorage.getItem('crm_lang') as AppLang | null;
    this.setLang(saved ?? 'en');
  }

  setLang(lang: AppLang): void {
    this._lang.set(lang);
    document.documentElement.lang = lang;
    document.documentElement.dir = lang === 'ar' ? 'rtl' : 'ltr';
    localStorage.setItem('crm_lang', lang);
  }

  toggleLang(): void {
    this.setLang(this._lang() === 'en' ? 'ar' : 'en');
  }
}
