import { Pipe, PipeTransform, inject } from '@angular/core';
import { I18nService } from '../services/i18n.service';
import { TRANSLATIONS } from '../i18n/translations';

@Pipe({
  name: 'translate',
  standalone: true,
  pure: false,
})
export class TranslatePipe implements PipeTransform {
  private readonly i18n = inject(I18nService);

  transform(key: string): string {
    const lang = this.i18n.lang();
    return TRANSLATIONS[key]?.[lang] ?? TRANSLATIONS[key]?.['en'] ?? key;
  }
}
