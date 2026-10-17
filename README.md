\# Planoria



Arkadaşlarla ve gruplarla birlikte plan yapmaya odaklanan bir ajanda uygulaması.

.NET MAUI ile yazıldı, verileri SQL Server'da tutar.



\## Özellikler

\- Kayıt ve giriş (şifreler BCrypt ile hash'lenir)

\- Günlük, haftalık, aylık ve yıllık görevler

\- Arkadaşlık sistemi: arama, istek, kabul, engelleme

\- Birebir sohbet; arkadaş olmayanlar için mesaj isteği

\- Grup sohbeti, grup davetleri, yönetici / yardımcı yönetici / üye rolleri

\- Lider onaylı, süreli grup görevleri (geri sayım sayacı, görevi tek kişi üstlenir)

\- Emoji avatar seçimi



\## Teknolojiler

.NET 9, .NET MAUI (XAML), Entity Framework Core 9 (Code First), SQL Server, BCrypt.Net-Next



\## Kurulum

1\. SQL Server Express'i kur ve TCP/IP'yi etkinleştir.

2\. `Planoria/Data/DbSecrets.example.txt` dosyasını `DbSecrets.cs` olarak aynı klasöre kopyala,

&#x20;  gerekirse sunucu adını kendi bilgisayarına göre değiştir.

3\. Package Manager Console'da `Update-Database` komutunu çalıştır.

4\. Windows hedefiyle çalıştır.



\## Bilinen sınırlamalar

\- Şu an yalnızca Windows hedefi test edildi; Android derlemesi henüz çözülmedi.

\- Uygulama veritabanına doğrudan bağlanıyor, yetki kontrolleri uygulama içinde yapılıyor.

&#x20; Bunlar sunucu tarafına taşınmalı.

\- Oturum bilgisi yerel depolamada tutuluyor (JWT yok).

\- Mesajlar anlık değil, sayfa yenilenince güncelleniyor.

\- Giriş denemesi sınırı yok.



\## Yol haritası

\- ASP.NET Core Web API + JWT ile istemci-sunucu mimarisine geçiş

\- SignalR ile anlık mesajlaşma ve okunmamış mesaj sayacı

\- Lider değerlendirme (yıldız puanı) ve yüksek puanlı liderlere "Danışmanlık Al" akışı

\- E-posta doğrulama

\- Android desteği

