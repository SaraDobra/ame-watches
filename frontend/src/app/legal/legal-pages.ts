export interface LegalSection {
  h: string;
  p: string[];
  list?: string[];
}

export interface LegalPage {
  title: string;
  description: string;
  updated: string;
  sections: LegalSection[];
}

const CONTACT = 'ame.accessories.ks@gmail.com';

export const LEGAL_PAGES: Record<string, LegalPage> = {
  privatesia: {
    title: 'Politika e privatësisë',
    description: 'Si i përdorim dhe i mbrojmë të dhënat tuaja kur porosisni te amé watches.',
    updated: '5 nëntor 2026',
    sections: [
      { h: 'Kush jemi', p: [`amé watches është dyqan online i orëve Casio dhe Q&Q. Për çdo pyetje rreth të dhënave tuaja na shkruani në ${CONTACT}.`] },
      {
        h: 'Çfarë të dhënash mbledhim',
        p: ['Mbledhim vetëm ato që na duhen për të dorëzuar porosinë tuaj:'],
        list: ['emrin dhe mbiemrin', 'numrin e telefonit', 'adresën e email-it', 'adresën dhe qytetin e dorëzimit', 'përmbajtjen e porosisë'],
      },
      { h: 'Pagesa me kartë', p: ['Kur paguani me kartë, të dhënat e kartës futen te ofruesi i pagesës, jo te ne. Ne nuk i shohim dhe nuk i ruajmë numrat e kartave.'] },
      {
        h: 'Pse i përdorim',
        p: ['Të dhënat përdoren për:'],
        list: ['përpunimin dhe dorëzimin e porosisë', 'dërgimin e email-it të konfirmimit', 'garancinë dhe kontaktin për porosinë tuaj'],
      },
      { h: 'Nuk i shesim', p: ['Të dhënat tuaja nuk i shesim dhe nuk i përdorim për reklama.'] },
      {
        h: 'Me kë i ndajmë',
        p: ['Të dhënat ndahen vetëm kur është e nevojshme për porosinë:'],
        list: ['me kurierin që e dorëzon porosinë (emri, telefoni, adresa)', 'me ofruesin e pagesës, kur paguani me kartë', 'me shërbimin e email-it që dërgon konfirmimet'],
      },
      { h: 'Sa i ruajmë', p: ['I ruajmë për sa kohë është e nevojshme për porosinë, për garancinë (1 vit pas dorëzimit) dhe për detyrimet tona ligjore.'] },
      {
        h: 'Shfletuesi juaj',
        p: [
          'Shportën e ruajmë në shfletuesin tuaj, që të mos humbasë kur e mbyllni faqen. Nuk përdorim cookie reklamimi ose gjurmimi.',
          'Fontet e faqes ngarkohen nga Google Fonts, prandaj shfletuesi juaj lidhet me serverët e Google për to.',
        ],
      },
      { h: 'Të drejtat tuaja', p: [`Mund të kërkoni të shihni, korrigjoni ose fshini të dhënat tuaja. Na shkruani në ${CONTACT} dhe do t'ju përgjigjemi.`] },
      { h: 'Ndryshime', p: ['Nëse e ndryshojmë këtë politikë, data e përditësimit më sipër ndryshon.'] },
    ],
  },

  kushtet: {
    title: 'Kushtet e përdorimit',
    description: 'Kushtet e blerjes në amé watches: çmimet, porosia, pagesa dhe anulimi.',
    updated: '5 nëntor 2026',
    sections: [
      { h: 'Të përgjithshme', p: ['Duke porositur në këtë faqe pranoni këto kushte. amé watches shet ora origjinale Casio dhe Q&Q.'] },
      { h: 'Çmimet', p: ['Çmimet janë në euro. Çmimi që shihni në shportë është çmimi që paguani; dërgesa është falas. Çmimi i porosisë mbetet ai që shihni në momentin kur e dërgoni.', 'Nëse gabimisht shfaqet një çmim i dukshëm i gabuar, ju njoftojmë para se ta konfirmojmë porosinë, dhe mund ta anuloni pa pasoja.'] },
      { h: 'Porosia', p: ['Pas dërgimit të porosisë merrni një email me numrin e saj (AW-XXXXXX) dhe një lidhje për ta ndjekur.', 'Stoku është i kufizuar. Nëse një orë nuk disponohet më, ju njoftojmë dhe nuk paguani asgjë.'] },
      { h: 'Pagesa', p: ['Mund të paguani në dorëzim ose me kartë. Porosia me kartë që nuk paguhet brenda 30 minutave anulohet dhe ora kthehet në stok.'] },
      { h: 'Anulimi', p: [`Para se porosia të dërgohet mund ta anuloni duke na shkruar në ${CONTACT} me numrin e porosisë.`] },
      { h: 'Të drejtat e faqes', p: ['Fotot dhe tekstet e faqes janë të amé watches dhe nuk lejohen të kopjohen pa lejen tonë.'] },
      { h: 'Ligji', p: ['Këto kushte rregullohen nga ligjet në fuqi në Republikën e Kosovës.'] },
    ],
  },

  'dergesa-pagesa': {
    title: 'Dërgesa dhe pagesa',
    description: 'Dërgesa falas dhe mënyrat e pagesës te amé watches.',
    updated: '5 nëntor 2026',
    sections: [
      { h: 'Dërgesa falas', p: ['Dërgesa është falas për çdo porosi. Orët i dërgojmë brenda Kosovës, në adresën që shkruani gjatë porosisë.'] },
      { h: 'Afati', p: ['Pasi ta pranojmë porosinë, ju kontaktojmë në telefon ose email për ta konfirmuar kohën e dorëzimit.'] },
      { h: 'Gjatë marrjes', p: ['Kontrolloni paketën kur e merrni. Nëse është dëmtuar ose nuk përputhet me porosinë, na njoftoni menjëherë në ' + CONTACT + '.'] },
      {
        h: 'Mënyrat e pagesës',
        p: ['Mund të zgjidhni një nga dy mënyrat:'],
        list: ['pagesë në dorëzim, kur e merrni orën', 'pagesë me kartë, e sigurt përmes ofruesit të pagesës'],
      },
    ],
  },

  'garancia-kthimi': {
    title: 'Garancia dhe kthimi',
    description: 'Garancia 1 vit nga amé watches dhe si mund ta ktheni një orë.',
    updated: '5 nëntor 2026',
    sections: [
      { h: 'Garancia 1 vit', p: ['Çdo orë ka garanci 1 vit nga amé watches, që fillon nga data e dorëzimit. Garancia mbulon defektet e prodhimit.'] },
      {
        h: 'Çfarë nuk mbulon garancia',
        p: [],
        list: ['dëmtimet nga rënia ose goditja', 'dëmtimet nga uji përtej rezistencës së shënuar te ora', 'hapjen ose riparimin nga persona të paautorizuar', 'konsumimin normal (p.sh. gërvishtje në xham, rripi)'],
      },
      { h: 'Si e përdorni garancinë', p: [`Na shkruani në ${CONTACT} me numrin e porosisë (AW-XXXXXX) dhe një përshkrim ose foto të problemit. Do t'ju tregojmë hapat për riparim ose zëvendësim.`] },
      {
        h: 'Kthimi',
        p: ['Mund ta ktheni orën brenda 14 ditëve nga marrja, me kusht që të jetë e papërdorur dhe në paketimin origjinal. Na shkruani para se ta ktheni.', 'Paratë i kthejmë brenda 14 ditëve pasi ta marrim orën. Kostoja e dërgimit të kthimit paguhet nga blerësi, përveç nëse ora ka defekt ose i është dërguar gabimisht.'],
      },
    ],
  },
};
