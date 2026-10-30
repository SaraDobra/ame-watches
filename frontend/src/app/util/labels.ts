const GENDERS: Record<string, string> = {
  Men: 'Burra',
  Women: 'Gra',
  Unisex: 'Unisex'
};

const TYPES: Record<string, string> = {
  Analog: 'Analoge',
  Digital: 'Dixhitale',
  AnalogDigital: 'Analoge dhe dixhitale'
};

export const genderLabel = (value: string): string => GENDERS[value] ?? value;
export const typeLabel = (value: string): string => TYPES[value] ?? value;
