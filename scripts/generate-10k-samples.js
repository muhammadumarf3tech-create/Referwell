const fs = require('fs');
const path = require('path');

const dir = path.join(__dirname, '..', 'frontend', 'public', 'samples', 'referral-import');
fs.mkdirSync(dir, { recursive: true });

const HEADER =
  'NhiNumber,PatientName,DateOfBirth,PatientEmail,PatientPhone,Gender,SpecialistType,Reason,Urgency,Status,ReceivedAt,AssignedToEmail,ReferringGpEmail,LegacyCaseNo';

const firstNames = [
  'Olivia', 'Noah', 'Amelia', 'Liam', 'Sophia', 'Mason', 'Isla', 'Ethan', 'Mia', 'Jack',
  'Ava', 'William', 'Charlotte', 'James', 'Harper', 'Lucas', 'Ella', 'Benjamin', 'Grace', 'Henry',
  'Ruby', 'Oscar', 'Zoe', 'Leo', 'Lily', 'Finn', 'Chloe', 'Archie', 'Emily', 'George',
  'Poppy', 'Harry', 'Ivy', 'Jacob', 'Sienna', 'Thomas', 'Willow', 'Daniel', 'Penelope', 'Samuel',
];
const lastNames = [
  'Bennett', 'Patel', 'Chen', 'OConnor', 'Ngata', 'Reid', 'Thompson', 'Walker', 'Fraser', 'Morrison',
  'Singh', 'Park', 'Lee', 'Brown', 'White', 'Green', 'Scott', 'Hall', 'Adams', 'King',
  'Clarke', 'Diaz', 'Mitchell', 'Carter', 'Evans', 'Hughes', 'Brooks', 'Price', 'Foster', 'Bailey',
  'Russell', 'Griffin', 'Hamilton', 'Murray', 'Cole', 'Reed', 'Hayes', 'Fox', 'Shaw', 'Barnes',
];
const specialties = [
  'Cardiology', 'Orthopedics', 'Neurology', 'Dermatology',
  'Oncology', 'Gastroenterology', 'Ophthalmology', 'Pulmonology',
];
const urgencies = ['Routine', 'SemiUrgent', 'Urgent'];
const statuses = ['Received', 'Triaged', 'Accepted', 'Declined', 'Booked', 'Completed'];
const genders = ['Female', 'Male'];
const assignees = ['nurse@referwell.com', 'admin@referwell.com'];
const gps = ['gp1@referwell.com', 'gp2@referwell.com'];
const reasons = {
  Cardiology: ['Exertional chest pain', 'Palpitations with syncope', 'Atrial fibrillation review', 'Heart failure optimisation', 'Abnormal ECG'],
  Orthopedics: ['Persistent knee pain', 'Shoulder dislocation', 'Hip osteoarthritis', 'Ankle instability', 'Back pain with sciatica'],
  Neurology: ['Recurrent migraines', 'New onset seizure', 'Parkinsonism features', 'TIA symptoms', 'Peripheral neuropathy'],
  Dermatology: ['Suspicious pigmented lesion', 'Widespread eczema', 'Psoriasis not responding', 'Chronic urticaria', 'Acne refractory'],
  Oncology: ['Breast lump assessment', 'Lymphadenopathy review', 'Suspected malignancy', 'Post-treatment follow-up', 'Abnormal imaging mass'],
  Gastroenterology: ['Iron deficiency anaemia', 'Rectal bleeding', 'Abdominal pain weight loss', 'IBD flare review', 'Dysphagia assessment'],
  Ophthalmology: ['Sudden visual field loss', 'Diabetic retinopathy', 'Cataract affecting driving', 'Glaucoma review', 'Red eye persistent'],
  Pulmonology: ['Chronic cough 3 months', 'COPD exacerbation', 'Hemoptysis investigation', 'Asthma poorly controlled', 'Suspected ILD'],
};

const pad = (n, w = 6) => String(n).padStart(w, '0');
const pick = (arr, i) => arr[i % arr.length];
const dob = (i) => {
  const y = 1940 + (i % 55);
  const m = String((i % 12) + 1).padStart(2, '0');
  const d = String((i % 28) + 1).padStart(2, '0');
  return `${y}-${m}-${d}`;
};
const received = (i, yearBase) => {
  const m = String((i % 12) + 1).padStart(2, '0');
  const d = String((i % 28) + 1).padStart(2, '0');
  const h = String(8 + (i % 10)).padStart(2, '0');
  const mi = String((i * 7) % 60).padStart(2, '0');
  return `${yearBase}-${m}-${d}T${h}:${mi}:00`;
};

function writeFile(filename, rows, rowFn) {
  const out = path.join(dir, filename);
  const fd = fs.openSync(out, 'w');
  fs.writeSync(fd, `${HEADER}\r\n`);
  const chunk = [];
  for (let i = 1; i <= rows; i++) {
    chunk.push(rowFn(i));
    if (chunk.length >= 500) {
      fs.writeSync(fd, `${chunk.join('\r\n')}\r\n`);
      chunk.length = 0;
    }
  }
  if (chunk.length) fs.writeSync(fd, `${chunk.join('\r\n')}\r\n`);
  fs.closeSync(fd);
  const size = fs.statSync(out).size;
  return { filename, rows, sizeKb: Math.round(size / 1024) };
}

const results = [];

results.push(
  writeFile('sample-10k-01-cardiology.csv', 10000, (i) => {
    const name = `${pick(firstNames, i)} ${pick(lastNames, i * 3)}`;
    const nhi = `C1${pad(i, 5)}`;
    const reason = pick(reasons.Cardiology, i);
    return [
      nhi, name, dob(i), `c1.${i}@example.com`, `+64 21 1${pad(i, 6)}`, pick(genders, i),
      'Cardiology', reason, pick(urgencies, i), pick(statuses, i), received(i, 2025),
      pick(assignees, i), pick(gps, i), `LEG-C1-${pad(i)}`,
    ].join(',');
  })
);

results.push(
  writeFile('sample-10k-02-mixed-specialty.csv', 10000, (i) => {
    const spec = pick(specialties, i);
    const name = `${pick(firstNames, i + 7)} ${pick(lastNames, i * 5)}`;
    const nhi = `M2${pad(i, 5)}`;
    const reason = pick(reasons[spec], i);
    return [
      nhi, name, dob(i + 2), `m2.${i}@example.com`, `+64 22 2${pad(i, 6)}`, pick(genders, i + 1),
      spec, reason, pick(urgencies, i + 1), pick(statuses, i + 2), received(i, 2025),
      pick(assignees, i + 1), pick(gps, i + 1), `LEG-M2-${pad(i)}`,
    ].join(',');
  })
);

results.push(
  writeFile('sample-10k-03-with-errors.csv', 10000, (i) => {
    const name = `${pick(firstNames, i + 3)} ${pick(lastNames, i * 2)}`;
    let nhi = `E3${pad(i, 5)}`;
    let urgency = pick(urgencies, i);
    let status = pick(statuses, i);
    let specialty = pick(specialties, i);
    let reason = pick(reasons[specialty], i);
    let assignee = pick(assignees, i);
    let gp = pick(gps, i);
    let dobVal = dob(i);
    let receivedAt = received(i, 2025);
    let legacy = `LEG-E3-${pad(i)}`;
    const err = i % 20;
    if (err === 0) { nhi = ''; reason = 'Missing NHI'; }
    else if (err === 1) { urgency = 'High'; reason = 'Bad urgency'; }
    else if (err === 2) { status = 'Pending'; reason = 'Bad status'; }
    else if (err === 3) { assignee = 'nobody@referwell.com'; reason = 'Bad assignee'; }
    else if (err === 4) { specialty = ''; reason = 'Missing specialty'; }
    else if (err === 5) { reason = ''; }
    else if (err === 6) { dobVal = '2099-01-01'; reason = 'Future DOB'; }
    else if (err === 7) { receivedAt = 'not-a-date'; reason = 'Bad received'; }
    else if (err === 8) { gp = 'unknown.gp@example.com'; reason = 'Bad GP'; }
    else if (err === 9 && i > 10) { legacy = `LEG-E3-${pad(i - 10)}`; reason = 'Dup legacy'; }

    return [
      nhi, name, dobVal, `e3.${i}@example.com`, `+64 27 3${pad(i, 6)}`, pick(genders, i),
      specialty, reason, urgency, status, receivedAt, assignee, gp, legacy,
    ].join(',');
  })
);

results.push(
  writeFile('sample-10k-04-historical.csv', 10000, (i) => {
    const spec = pick(specialties, i + 3);
    const name = `${pick(firstNames, i + 11)} ${pick(lastNames, i * 7)}`;
    const nhi = `H4${pad(i, 5)}`;
    const year = 2023 + (i % 3);
    const status = pick(['Completed', 'Declined', 'Booked', 'Accepted', 'Triaged', 'Completed'], i);
    return [
      nhi, name, dob(i + 5), `h4.${i}@example.com`, `+64 29 4${pad(i, 6)}`, pick(genders, i),
      spec, pick(reasons[spec], i), pick(urgencies, i), status, received(i, year),
      pick(assignees, i), pick(gps, i), `LEG-H4-${pad(i)}`,
    ].join(',');
  })
);

results.push(
  writeFile('sample-10k-05-routine-volume.csv', 10000, (i) => {
    const spec = pick(specialties, i + 1);
    const name = `${pick(firstNames, i + 19)} ${pick(lastNames, i * 11)}`;
    const nhi = `R5${pad(i, 5)}`;
    return [
      nhi, name, dob(i + 9), `r5.${i}@example.com`, `+64 21 5${pad(i, 6)}`, pick(genders, i),
      spec, pick(reasons[spec], i + 2), 'Routine', pick(['Received', 'Triaged', 'Accepted', 'Booked'], i),
      received(i, 2025), 'nurse@referwell.com', pick(gps, i), `LEG-R5-${pad(i)}`,
    ].join(',');
  })
);

for (const old of [
  'sample-01-valid-cardiology.csv',
  'sample-02-valid-mixed-specialty.csv',
  'sample-03-with-validation-errors.csv',
  'sample-04-existing-patients.csv',
  'sample-05-historical-statuses.csv',
]) {
  const p = path.join(dir, old);
  if (fs.existsSync(p)) fs.unlinkSync(p);
}

console.log(JSON.stringify(results, null, 2));
