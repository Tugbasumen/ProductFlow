# ProductFlow

ProductFlow, ürün ve stok yönetimi süreçlerini yönetmek için geliştirilen ASP.NET Core Web API projesidir. Proje; katmanlı mimari, REST API tasarımı, Entity Framework Core, otomatik testler ve CI süreçlerini uygulamalı olarak geliştirmek amacıyla oluşturulmaktadır.

## Teknolojiler

- C# ve .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- Microsoft SQL Server
- Swagger / OpenAPI
- xUnit
- SQLite ve EF Core InMemory ile testler
- GitHub Actions

## Mevcut özellikler

- Ürün oluşturma, listeleme ve detay görüntüleme
- Ürün bilgilerini güncelleme
- Stok miktarını artırma veya azaltma
- Ürünü pasifleştirme
- Ürün kodu benzersizliği kontrolü
- İstek doğrulama ve standart hata yanıtları
- Birim ve API entegrasyon testleri
- GitHub Actions ile otomatik derleme ve test

## Mimari

Çözüm, sorumlulukları ayrı katmanlara bölerek düzenlenmiştir:

- **ProductFlow.Domain:** Temel iş varlıkları ve iş kuralları
- **ProductFlow.Application:** DTO'lar, servis arayüzleri ve uygulama kuralları
- **ProductFlow.Infrastructure:** Veritabanı erişimi ve servis uygulamaları
- **ProductFlow.Api:** HTTP uç noktaları, Swagger ve hata yönetimi
- **ProductFlow.UnitTests:** Birim ve API entegrasyon testleri

## Gereksinimler

- .NET 8 SDK
- Microsoft SQL Server
- Git

## Kurulum ve çalıştırma

1. Depoyu klonlayın:

   ```bash
   git clone https://github.com/Tugbasumen/ProductFlow.git
   cd ProductFlow
   ```

2. `ProductFlow.Api/appsettings.json` dosyasındaki `ProductFlowDatabase` bağlantı ayarını kendi SQL Server ortamınıza göre düzenleyin.

3. Paketleri geri yükleyin:

   ```bash
   dotnet restore
   ```

4. Veritabanı migration'larını uygulayın:

   ```bash
   dotnet ef database update --project ProductFlow.Infrastructure --startup-project ProductFlow.Api
   ```

5. API'yi çalıştırın:

   ```bash
   dotnet run --project ProductFlow.Api
   ```

6. Uygulama çalışırken konsolda belirtilen adres üzerinden Swagger arayüzüne erişin.

### Docker ile çalıştırma

Aynı ortamı hızlıca yeniden üretebilmek için Docker Compose kullanılabilir:

```bash
docker compose up --build
```

Bu komut:

- SQL Server konteynerini başlatır
- API uygulamasını derleyip çalıştırır
- `http://localhost:8080/swagger` adresinden Swagger erişimini açar

Açık SQL Server bağlantı dizesi:

```text
Server=db;Database=ProductFlowDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;
```

## API uç noktaları

| Metot | Uç nokta                        | Açıklama                   |
| ----- | ------------------------------- | -------------------------- |
| GET   | `/api/products`                 | Ürünleri listeler          |
| GET   | `/api/products/{id}`            | Ürün detayını getirir      |
| POST  | `/api/products`                 | Yeni ürün oluşturur        |
| PUT   | `/api/products/{id}`            | Ürün bilgilerini günceller |
| PATCH | `/api/products/{id}/stock`      | Stok miktarını değiştirir  |
| PATCH | `/api/products/{id}/deactivate` | Ürünü pasifleştirir        |

## Testler

Tüm testleri çalıştırmak için:

```bash
dotnet test
```

Release modunda derlemek için:

```bash
dotnet build --configuration Release
```

## Sürekli Entegrasyon (CI)

GitHub Actions, `main` dalına yapılan push ve Pull Request işlemlerinde projeyi otomatik olarak derler ve testleri çalıştırır.

İş akışı dosyası: `.github/workflows/dotnet-ci.yml`

## Geliştirme yol haritası

- Kategori yönetimi
- Müşteri ve sipariş modülleri
- İlişkisel veri modeli ve transaction yönetimi
- Test kapsamının genişletilmesi
- Docker ve React arayüzü
- İleri aşamada Redis ve RabbitMQ

## Durum

Proje aktif olarak geliştirilmektedir. Yeni özellikler ve mimari iyileştirmeler aşamalı olarak eklenecektir.
