# Agent Workflow — Eight Months of Silence

Alur kerja terstruktur yang wajib diikuti agent untuk setiap task (desain, narasi, sistem, level, art brief, kode, dokumentasi).

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. INTAKE                                                        │
│    - Pahami task dari user/pemilik proyek                        │
│    - Klasifikasikan tipe task (naratif / sistem / level / UI /   │
│      art-audio / teknis / scoping / QA)                          │
└───────────────────────────────┬─────────────────────────────────┘
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. LOAD CONTEXT                                                   │
│    - Baca docs/00-index.md                                        │
│    - Buka HANYA file docs/ yang relevan dengan tipe task           │
│    - Baca peran terkait di agents/ jika ada                       │
└───────────────────────────────┬─────────────────────────────────┘
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. VALIDASI AWAL (Gate)                                           │
│    - Cek terhadap 3 Pilar Desain (docs/02)                       │
│    - Cek terhadap Scope (docs/10) → dalam scope / butuh eskalasi? │
│    - Cek apakah task menyentuh item TBD (docs/12)                 │
│    - Jika gagal gate → hentikan, eskalasi sesuai                  │
│      rules/tbd-escalation-rules.md                                 │
└───────────────────────────────┬─────────────────────────────────┘
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. EKSEKUSI                                                        │
│    - Kerjakan task sesuai peran (lihat agents/<peran>.md)          │
│    - Jaga konsistensi lintas dokumen (R7)                          │
│    - Tandai asumsi & item TBD yang tersentuh secara eksplisit      │
└───────────────────────────────┬─────────────────────────────────┘
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. SELF-CHECK                                                      │
│    - Cocokkan hasil terhadap Definition of Done (docs/11)          │
│    - Cek risiko terkait (docs/11) — apakah solusi memperbesar      │
│      risiko yang sudah teridentifikasi?                            │
│    - Cek rating/batas konten (docs/01) jika task menyentuh          │
│      kekerasan/gore                                                │
└───────────────────────────────┬─────────────────────────────────┘
                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. LAPORAN                                                          │
│    - Ringkas apa yang dikerjakan + rujukan docs/ yang dipakai      │
│    - Daftar asumsi eksplisit                                        │
│    - Daftar item TBD yang tersentuh & butuh keputusan               │
│    - Tandai bagian yang berstatus [USULAN BARU] bila ada            │
└─────────────────────────────────────────────────────────────────┘
```

## Contoh Penerapan Singkat

**Task**: "Buatkan desain evidence baru untuk Bab 2 (motel)."
1. Intake → tipe task: naratif + sistem (evidence).
2. Load context → `docs/03-core-gameplay-systems.md` (Aturan Evidence), `docs/05-chapter-structure.md` (Bab 2).
3. Validasi → evidence harus sejalan Pilar 2 (Inspection), tidak melanggar scope.
4. Eksekusi → buat evidence dengan 5 atribut wajib: nama, deskripsi, hotspot, fakta, dependency, consequence.
5. Self-check → apakah evidence ini memberi konsekuensi gameplay/naratif nyata (bukan gimmick, sesuai risiko #1 di `docs/11`)?
6. Laporan → sebutkan rujukan bab & aturan yang dipakai, tandai jika evidence ini "USULAN BARU" di luar apa yang tersurat di GDD asli.

## Kapan Menggunakan `ask` / Eskalasi ke User
- Task menyentuh item TBD baru (belum tercatat) → **wajib** eskalasi sebelum lanjut.
- Task berpotensi melanggar scope (`docs/10`) → **wajib** eskalasi.
- Task ambigu tapi tidak menyentuh pilar/scope/TBD → agent boleh memilih interpretasi paling masuk akal, nyatakan asumsi, dan lanjutkan (tidak perlu berhenti total).
