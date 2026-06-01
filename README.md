# 🎵 MP3Converter - Advanced YouTube Media Downloader

Modern, kullanıcı dostu ve karanlık tema (Dark UI) arayüzüne sahip; YouTube üzerindeki tekli videoları veya tüm oynatma listelerini (Playlist) asenkron olarak yüksek kalitede indiren ve dönüştüren profesyonel bir masaüstü uygulamasıdır.

Uygulama, sistem kaynaklarını minimum düzeyde kullanarak, arayüzü kilitlemeden arka planda asenkron olarak çalışacak şekilde optimize edilmiştir.

---

## 🚀 Doğrudan Kurulum ve Çalıştırma (Son Kullanıcı)

Uygulamayı bilgisayarınıza kodlarla uğraşmadan direkt kurmak için aşağıdaki butona tıklayarak Türkçe Kurulum Sihirbazını indirebilirsiniz:

👉 **[📥 MP3Converter Türkçe Windows Kurulum Paketini İndir (v1.0.0)](https://github.com/kedyflex/MP3Converter-WPF/releases/download/v1.0/MP3Converter_Setup_win-x64.exe)**

---

## ✨ Öne Çıkan Özellikler

- **🎵 Akıllı Ses & Video Dönüştürme:** YouTube linklerini analiz ederek en yüksek ses kalitesinde (160kbps MP3) veya optimize edilmiş video formatında (MP4) hızlıca indirir.
- **📋 Akıllı Pano (Clipboard) Entegrasyonu:** Siz arkada bir YouTube linki kopyaladığınızda, uygulamaya döndüğünüz an link otomatik olarak algılanır ve Regex süzgecinden geçirilerek panele yapıştırılır.
- **🖼️ Canlı Önizleme (Preview Panel):** Link yapıştırıldığı an video başlığı, kanal adı, video süresi ve yüksek çözünürlüklü kapak görseli (Thumbnail) arayüzde anlık olarak yüklenir.
- **🎼 Oynatma Listesi (Playlist) Desteği:** Tek bir şarkı yerine tüm oynatma listesi linklerini yapıştırarak, listedeki tüm medyaları sırasıyla ve otomatik olarak indirebilirsiniz.
- **🧹 Gelişmiş Kalıntı Engelleme Sistemi:** İndirme işlemi kullanıcı tarafından iptal edildiğinde veya yarıda kesildiğinde, diskte oluşan geçici `part/webm` dosyalarını anında tespit ederek sistemi otomatik temizler; arkasında çöp bırakmaz.

---

## 🛠️ Teknolojiler ve Altyapı

- **Geliştirme Ortamı:** .NET 10 & WPF (C# / XAML)
- **Çekirdek Motorlar (Core Engines):**
  - [yt-dlp](https://github.com/yt-dlp/yt-dlp) - YouTube ve diğer platformlardan asenkron olarak en yüksek kalitede video/ses metadatası ve akışı (stream) çekmek için kullanılan gelişmiş CLI aracı.
  - [FFmpeg & ffprobe](https://ffmpeg.org/) - İndirilen ses ve video akışlarını arka planda arayüzü kilitlemeden birleştiren (merger), dönüştüren ve kodlayan (encoder) endüstri standardı multimedya işlem motoru.

---

## 📸 Ekran Görüntüleri

<table width="100%" border="0">
  <tr>
    <td width="50%" align="center">
      <img src="https://github.com/user-attachments/assets/a0ab7164-5cb5-47e8-85b6-959278c2bd32" alt="MP3Converter Arayüz" width="100%"/>
    </td>
    <td width="50%" align="center">
      <img src="https://github.com/user-attachments/assets/b10197db-df70-428d-957c-c4aed1907c76" alt="MP3Converter Bilgilendirme" width="100%"/>
    </td>
  </tr>
  <tr>
    <td width="50%" align="center">
      <img src="https://github.com/user-attachments/assets/7d11d0a7-9d9b-4c5e-b101-06cacaab22a8" alt="MP3Converter Preview Görünümü" width="100%"/>
    </td>
    <td width="50%" align="center">
      <img src="https://github.com/user-attachments/assets/d5988667-7f4f-409c-b34e-69ffef5c763a" alt="MP3Converter Medya İndirme" width="100%"/>
    </td>
  </tr>
</table>

---

## 💻 Geliştiriciler İçin Derleme Kurulumu

Projeyi kendi bilgisayarınızda geliştirmek veya derlemek isterseniz:

1. Bu depoyu yerel bilgisayarınıza klonlayın:
   ```bash
   git clone https://github.com/kedyflex/MP3Converter-WPF.git
Projeyi Visual Studio ile açın.

.NET 10 SDK'sının bilgisayarınızda kurulu olduğundan emin olun.

Projeyi Release modunda derleyerek (Build) kendi .exe çıktınızı alabilirsiniz.

---

📜 Lisans
Bu proje MIT Lisansı altında lisanslanmıştır. Kaynak göstermek şartıyla dilediğiniz gibi geliştirebilir, kişisel veya ticari projelerinizde kullanabilirsiniz.
