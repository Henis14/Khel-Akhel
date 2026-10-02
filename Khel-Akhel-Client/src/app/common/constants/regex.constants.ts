export const REGEX_PATTERNS = {
  NAME: /^[A-Za-z]{2,50}$/,
  EMAIL: /^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$/,
  MOBILE: /^[6-9][0-9]{9}$/,
  PASSWORD: /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
  SKU: /^[A-Za-z0-9_-]{3,50}$/,
  PINCODE: /^[1-9][0-9]{5}$/,
  ALPHA_SPACE: /^[A-Za-z ]{2,100}$/,
  ACCOUNT_NO: /^DRC[0-9]{6}$/,
  PRODUCT_CODE: /^DRP[0-9]{6}$/,
  CITY: /^[A-Za-z][A-Za-z .'-]{1,99}$/
};
