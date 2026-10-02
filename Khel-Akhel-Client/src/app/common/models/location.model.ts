export interface CountryResponse {
  id: number;
  countryName: string;
  countryCode: string;
  phoneCode: string;
}

export interface StateResponse {
  id: number;
  countryId: number;
  stateName: string;
  stateCode: string;
}
