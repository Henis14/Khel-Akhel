import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export class CustomValidators {
  /**
   * Rejects null, undefined, empty string, or string with only whitespace.
   */
  static requiredNonEmpty(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (value === null || value === undefined) {
      return { required: true };
    }
    if (typeof value === 'string' && value.trim().length === 0) {
      return { required: true };
    }
    return null;
  }

  /**
   * Validates pattern after trimming whitespace (for non-password fields).
   */
  static pattern(regex: RegExp, errorKey: string = 'pattern'): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value && value !== 0) {
        return null;
      }
      const stringVal = typeof value === 'string' ? value.trim() : String(value);
      if (!regex.test(stringVal)) {
        return { [errorKey]: true };
      }
      return null;
    };
  }

  /**
   * Validates raw pattern WITHOUT trimming whitespace (for password fields).
   */
  static rawPattern(regex: RegExp, errorKey: string = 'pattern'): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (!value && value !== 0) {
        return null;
      }
      const stringVal = String(value);
      if (!regex.test(stringVal)) {
        return { [errorKey]: true };
      }
      return null;
    };
  }

  /**
   * Validates max decimal places (e.g. 2 for currency).
   */
  static maxDecimalPlaces(maxDecimals: number = 2): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      const value = control.value;
      if (value === null || value === undefined || value === '') {
        return null;
      }
      const numStr = String(value);
      if (numStr.includes('.')) {
        const decimalPart = numStr.split('.')[1];
        if (decimalPart && decimalPart.length > maxDecimals) {
          return { maxDecimalPlaces: true };
        }
      }
      return null;
    };
  }

  /**
   * Cross-field validator ensuring Selling Price <= MRP.
   */
  static sellingPriceLessOrEqualMrp(mrpKey: string = 'mrp', priceKey: string = 'sellingPrice'): ValidatorFn {
    return (group: AbstractControl): ValidationErrors | null => {
      const mrp = group.get(mrpKey)?.value;
      const price = group.get(priceKey)?.value;
      if (mrp !== null && mrp !== undefined && price !== null && price !== undefined) {
        if (Number(price) > Number(mrp)) {
          return { sellingPriceGreaterThanMrp: true };
        }
      }
      return null;
    };
  }

  /**
   * Validates non-negative number.
   */
  static nonNegativeNumber(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const num = Number(value);
    if (isNaN(num) || num < 0) {
      return { nonNegativeNumber: true };
    }
    return null;
  }

  /**
   * Validates positive number (> 0).
   */
  static positiveNumber(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const num = Number(value);
    if (isNaN(num) || num <= 0) {
      return { positiveNumber: true };
    }
    return null;
  }

  /**
   * Validates non-negative integer (>= 0 and integer).
   */
  static nonNegativeInteger(control: AbstractControl): ValidationErrors | null {
    const value = control.value;
    if (value === null || value === undefined || value === '') {
      return null;
    }
    const num = Number(value);
    if (isNaN(num) || num < 0 || !Number.isInteger(num)) {
      return { nonNegativeInteger: true };
    }
    return null;
  }
}
