# AGENTS.md — Panduan Agent untuk Proyek "Eight Months of Silence"

Dokumen ini adalah **aturan utama (root rules)** untuk setiap agent (Claude Code, subagent, atau kontributor AI lain) yang bekerja di repo game ini. File ini WAJIB dibaca sebelum agent melakukan pekerjaan apa pun: menulis desain, membuat sistem, membuat konten naratif, level, UI, art brief, atau kode.

Sumber kebenaran tunggal (single source of truth) untuk desain adalah **Game Design Document (GDD)** yang sudah dipecah menjadi file-file konteks di folder `docs/`. Jangan pernah berasumsi, mengarang, atau "mengisi celah" desain tanpa merujuk `docs/` atau menandainya sebagai TBD (lihat `rules/tbd-escalation-rules.md`).

## 1. Struktur Repo Ini

```
AGENTS.md                  <- kamu sedang membacanya
docs/                       <- pecahan GDD per topik (context, read-only secara desain)
rules/                       <- aturan kepatuhan & eskalasi
workflows/                   <- alur kerja terstruktur untuk agent
agents/                       <- definisi peran/subagent per disiplin
```

## 2. Prinsip Kepatuhan (Non-Negotiable)

1. **GDD adalah kontrak.** Setiap keputusan desain, sistem, atau konten baru harus bisa ditelusuri ke salah satu dokumen di `docs/`. Jika tidak ada dasarnya, agent **berhenti dan bertanya**, bukan menebak.
2. **Tiga Pilar Desain tidak bisa dilanggar** (lihat `docs/02-design-pillars-vision.md`): Grounded Horror Movement, Inspection sebagai Mekanik Utama, Narrative Investigation. Fitur baru yang bertentangan dengan pilar ini harus ditolak atau dieskalasi.
3. **Scope terkunci sesuai `docs/10-production-scope.md`.** Fitur yang eksplisit "Di Luar Scope Awal" (open world, multiplayer, procedural generation, skill tree, full voice acting, console release, dll.) tidak boleh diimplementasikan tanpa keputusan eksplisit dari pemilik proyek.
4. **Isu TBD tidak boleh diselesaikan sepihak oleh agent.** Semua item di `docs/12-references-and-open-tbd.md` harus tetap ditandai TBD dalam output sampai pemilik proyek memutuskan. Agent boleh mengusulkan opsi, tidak boleh memutuskan atas nama desainer.
5. **Anti-frustrasi rules bersifat wajib**, bukan saran: critical evidence tidak boleh soft-lock, objective item wajib punya recovery, puzzle tidak boleh bergantung pixel-hunting (lihat `docs/03-core-gameplay-systems.md`).
6. **Rating usia dan batas konten** (`docs/01-project-overview.md`) mengikat semua keputusan gore/kekerasan: target utama 16–18, versi 13+ hanya sah bila darah/luka/eksekusi/kematian ditampilkan implisit.
7. **Konsistensi naratif dan karakter wajib dijaga** — nama, timeline, dan detail fisik karakter di `docs/04-narrative-and-characters.md` dan `docs/05-chapter-structure.md` adalah kanon; jangan mengubahnya secara implisit saat menulis konten baru.

## 3. Cara Agent Wajib Bekerja

Ikuti `workflows/agent-workflow.md` untuk setiap task. Ringkasan singkat:

1. **Load context** — baca `docs/00-index.md`, lalu buka hanya file `docs/` yang relevan dengan task (jangan muat semua sekaligus jika tidak perlu).
2. **Validasi terhadap pilar & scope** sebelum mulai membuat apa pun.
3. **Kerjakan task** sesuai peran yang relevan di `agents/`.
4. **Self-check** terhadap `docs/11-risks-and-dod.md` (Definition of Done) sebelum melaporkan selesai.
5. **Laporkan asumsi & TBD** yang tersentuh oleh task secara eksplisit di akhir laporan kerja.

## 4. Larangan Eksplisit untuk Agent

- Jangan menciptakan lore, karakter, lokasi, atau mekanik baru yang tidak berasal dari `docs/` tanpa menandai jelas sebagai **[USULAN BARU — perlu approval]**.
- Jangan mengubah numbering bab, timeline (26 Mei / 15 Oktober), atau nama karakter tanpa instruksi eksplisit.
- Jangan menyelesaikan angka tuning (stamina, damage, kecepatan, dsb.) yang berstatus TBD — biarkan tetap TBD atau usulkan rentang dengan alasan, jangan finalisasi sepihak.
- Jangan menambahkan fitur combat berulang, grinding, loot rarity, atau sistem RPG yang bertentangan dengan `docs/03-core-gameplay-systems.md` §8.1.
- Jangan menulis dialog voice-over penuh sebagai default — voice direction awal adalah text-only (stretch goal saja untuk voice-over).

## 5. Eskalasi

Jika sebuah task membutuhkan keputusan yang tidak ada di `docs/`, agent harus:
1. Mengecek `docs/12-references-and-open-tbd.md` — apakah ini item TBD yang sudah diketahui?
2. Jika ya: kerjakan bagian lain, tandai bagian yang bergantung pada TBD ini, dan ajukan opsi ke pemilik proyek.
3. Jika tidak (isu baru): berhenti, jelaskan konflik/gap, dan minta arahan sebelum melanjutkan. Lihat `rules/tbd-escalation-rules.md`.

## 6. Bahasa & Gaya

- Dokumen desain dan narasi ditulis dalam Bahasa Indonesia, konsisten dengan GDD asli.
- Istilah teknis Unity/engine boleh tetap dalam Bahasa Inggris (mis. ScriptableObject, hotspot, raycast).
- Nama sistem, item, dan UI label mengikuti istilah yang sudah dipakai di GDD (Evidence Log, Hand Slot, Pocket Slot, dll.) — jangan menerjemahkan ulang atau mengganti nama tanpa alasan.
