# ProductFlow

ProductFlow, küçük ve orta ölçekli işletmeler için ürün kataloğu, stok hareketi, müşteri ve sipariş süreçlerini tek bir uygulamada birleştiren full-stack envanter yönetimi projesidir.

## Özellikler

- Ürünleri kategoriye göre yönetme; arama, sıralama ve sayfalama
- Stok giriş/çıkış hareketlerini kayıt altına alma ve kritik stokları izleme
- Müşteri oluşturma, güncelleme ve pasife alma
- Çok kalemli sipariş oluşturma, onaylama ve iptal etme
- Sipariş iptalinde stokları transaction içinde geri yükleme
- ASP.NET Core API ve React/TypeScript dashboard
- API doğrulaması, merkezi hata yanıtları ve otomatik testler
- JWT tabanlı authentication ve Admin/Manager/Operator rol politikaları

## Teknoloji ve Mimari

- **Backend:** C#, .NET 8, ASP.NET Core Web API
- **Veri erişimi:** Entity Framework Core 8, SQL Server
- **Frontend:** React 19, TypeScript, Vite
- **Test:** xUnit, ASP.NET Core integration tests, SQLite/InMemory test veritabanları
- **CI:** GitHub Actions ile .NET testleri ve frontend production build
- **Çalıştırma:** Docker Compose

Çözüm katmanları:

- `ProductFlow.Domain`: varlıklar ve iş kuralları
- `ProductFlow.Application`: DTO'lar ve servis arayüzleri
- `ProductFlow.Infrastructure`: EF Core ve servis uygulamaları
- `ProductFlow.Api`: REST uç noktaları, Swagger ve hata middleware'i
- `ProductFlow.UnitTests`: birim ve API entegrasyon testleri
- `client`: React dashboard

## Gereksinimler

- .NET 8 SDK
- Node.js 22 veya üzeri ve npm
- Docker Desktop / Docker Compose
- Migration komutları için `dotnet-ef` aracı

## Hızlı Başlangıç

### Tek komutla tam uygulama (Docker)

`.env.example` dosyasını `.env` olarak kopyalayın ve tüm örnek değerleri kendinize ait güçlü parolalarla değiştirin. `.env` Git tarafından ignore edilir.

```bash
docker compose up --build -d
```

Compose SQL Server hazır olana kadar bekler; API açılışta migration'ları uygular ve bootstrap Admin hesabını oluşturur. React arayüzü <http://localhost>, API/Swagger ise <http://localhost:8080/swagger> adresindedir.

### Yerel geliştirme

Önce SQL Server'ı başlatıp veritabanını hazırlayın:

```powershell
docker compose --env-file .env up -d db
dotnet user-secrets init --project ProductFlow.Api
dotnet user-secrets set "ConnectionStrings:ProductFlowDatabase" "Server=localhost,1433;Database=ProductFlowDb;User Id=sa;Password=<SQL_PASSWORD>;TrustServerCertificate=True;" --project ProductFlow.Api
dotnet user-secrets set "Jwt:SigningKey" "<at-least-32-random-bytes>" --project ProductFlow.Api
dotnet user-secrets set "BootstrapAdmin:Email" "admin@example.com" --project ProductFlow.Api
dotnet user-secrets set "BootstrapAdmin:Password" "<strong-password>" --project ProductFlow.Api
dotnet run --project ProductFlow.Api --launch-profile http
```

### React arayüzü

API `5218` portunda çalışırken, başka bir terminalde:

```powershell
cd client
npm ci
npm run dev
```

Arayüz: <http://localhost:5173>. İlk Admin hesabı yukarıda tanımladığınız e-posta ve parola ile giriş yapar; yeni kullanıcıları `POST /api/auth/users` üzerinden Admin rolüyle oluşturabilirsiniz.

## Test ve Build

Backend testleri:

```powershell
dotnet test ProductFlow.sln --configuration Release
```

Frontend testleri ve production build:

```powershell
cd client
npm ci
npm test
npm run lint
npm run build
```

Her iki kontrol de GitHub Actions üzerinde push ve pull request sırasında çalışır.

## Önemli API Uç Noktaları

| Alan | Uç noktalar |
| --- | --- |
| Kimlik | `POST /api/auth/login`, `GET /api/auth/me`, Admin: `POST /api/auth/users` |
| Ürünler | `GET/POST /api/products`, `GET/PUT /api/products/{id}`, `PATCH /api/products/{id}/stock`, `PATCH /api/products/{id}/deactivate` |
| Kategoriler | `GET/POST /api/categories`, `GET/PUT /api/categories/{id}`, `PATCH /api/categories/{id}/deactivate` |
| Müşteriler | `GET/POST /api/customers`, `GET/PUT /api/customers/{id}`, `PATCH /api/customers/{id}/deactivate` |
| Siparişler | `GET/POST /api/orders`, `GET /api/orders/{id}`, `PATCH /api/orders/{id}/confirm`, `PATCH /api/orders/{id}/cancel` |
| Stok hareketleri | `GET/POST /api/stockmovements`, `GET /api/products/critical-stock` |

## Geliştirme Notları

- Geliştirme veritabanı kimlik bilgilerini ve production sırlarını repoya eklemeyin; ortam değişkeni veya secret store kullanın.
- Docker Compose için `.env.example` içindeki tüm placeholder değerleri değiştirin; özellikle SQL parolası, JWT signing key ve bootstrap Admin parolasını tekrar kullanmayın.
- JWT signing key en az 32 byte olmalı; bootstrap admin parolası Identity kurallarını karşılamalıdır.
- Üretim öncesi HTTPS termination, yedekleme, izleme ve rate limiting yapılandırılmalıdır.