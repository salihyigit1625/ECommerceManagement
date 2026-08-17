# 🛒 E-Commerce Management System
### Sysmond AX ERP & e-Fatura Entegrasyonlu Kurumsal API Platformu

.NET 10 üzerinde geliştirilmiş, kurumsal tasarım kalıpları, dinamik yetkilendirme, dağıtık önbellek stratejileri ve **Sysmond AX ERP / e-Fatura** dış servis entegrasyonuna sahip yüksek performanslı bir RESTful API platformu.

![.NET](https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-13-239120?style=flat-square&logo=csharp)
![SQL Server](https://img.shields.io/badge/SQL_Server-EF_Core_10-CC2927?style=flat-square&logo=microsoftsqlserver)
![Tests](https://img.shields.io/badge/tests-19%20passed-brightgreen?style=flat-square)

---

## 📌 Proje Hakkında

Bu proje, klasik bir CRUD API'nin ötesinde; **gerçek bir ERP sistemiyle çift yönlü, atomik ve durum makinesi (state machine) tabanlı senkronizasyon** gerektiren bir e-ticaret arka uç sistemidir. Odak noktası, sadece veri saklamak değil; sipariş, stok ve faturalama süreçlerinin yerel veritabanı ile Sysmond AX ERP arasında tutarlı, denetlenebilir ve hataya dayanıklı şekilde akmasını sağlamaktır.

---

## 🏛️ Mimari ve Proje Yapısı

Proje, **N-Tier Architecture** ve **Domain-Driven Design (DDD)** prensipleriyle sorumlulukları net şekilde ayrılmış katmanlar halinde tasarlanmıştır:

```text
├── src/
│   ├── ECommerceManagement.Domain          # Entities, Enum'lar, veritabanı sabitleri
│   ├── ECommerceManagement.Application     # İş mantığı, DTO'lar, servis arayüzleri, AutoMapper profilleri
│   ├── ECommerceManagement.Repository      # Generic Repository, Unit of Work, EF Core DbContext
│   ├── ECommerceManagement.Infrastructure  # JWT provider, dağıtık cache, authorization handler'lar
│   ├── ECommerceManagement.Api             # RESTful controller'lar, middleware, rate limiting
│   └── ECommerceManagement.Tests           # xUnit + Moq + FluentAssertions testleri
│
└── integrations/
    └── SysmondAx.Integration               # ERP HTTP istemcisi, payload modelleri, entegrasyon servisleri
```

Dış entegrasyon kodunun ayrı bir katmanda (`SysmondAx.Integration`) izole edilmesi, çekirdek iş mantığının ERP'nin API sözleşmesine bağımlı kalmamasını sağlar — ERP tarafı değişse bile `Application` katmanı büyük ölçüde etkilenmez.

---

## 🔐 Güvenlik ve Yetkilendirme

Sistem, tek katmanlı bir rol kontrolünden çok daha ayrıntılı bir yetkilendirme modeli kullanır:

- **Policy-Based Dynamic Authorization (RBAC + PBAC):** Rol bazlı izinlerin üzerine, kullanıcıya özel izinler tanımlanabilir ve gerektiğinde rol yetkilerini **override** edebilir. Bu, "aynı rolde ama farklı yetkilerde kullanıcı" senaryolarını veritabanı şeması değiştirmeden çözer.
- **JWT Bearer Authentication:** Kimlik doğrulama tamamen token tabanlıdır; stateless ve ölçeklenebilir bir yapı sunar.
- **Distributed Permission Caching:** İzin kontrolleri her istekte veritabanına gitmek yerine Redis / `IDistributedCache` üzerinden yapılır. Bir kullanıcının yetkileri güncellendiğinde ilgili cache girdisi otomatik olarak **invalidate** edilir — yani stale (bayat) yetki verisiyle çalışma riski yoktur.
- **Rate Limiting:** ASP.NET Core'un yerleşik rate limiting altyapısı ile (Fixed/Sliding Window) API kötüye kullanıma karşı korunur.
- **Audit Logging:** Stok üzerindeki her hareket (satış, iade, kargo, manuel düzeltme) `ProductMovement` tablosuna `Entry`/`Exit` tipiyle loglanır — bu sayede "stok neden değişti" sorusu her zaman geriye dönük olarak cevaplanabilir.
  
---

## ⚡ Sysmond AX ERP & e-Fatura Entegrasyonu

Bu proje asıl karmaşıklığını, yerel sistem ile dış ERP'nin **iki ayrı source-of-truth** olarak senkron kalması gereken senaryolarda gösteriyor:

```text
[ E-Commerce API ]  ──── HTTP Client ────  [ Sysmond AX ERP ]
      │ (Yerel DB)                              (Bulut / On-Prem)
      ├─ JIT Statü Kontrolü ────── GET  /order-query/orders
      ├─ Çift Yönlü Sipariş Senk. ─ GET  /order-query/{id}/items
      ├─ Tek Adımlı Faturalama ──── POST /outgoing-invoice/draft
      ├─ Stok/Fiyat Eşleme ──────── POST /outgoing-invoice/item
      └─ Sipariş Durum Geçişi ───── PUT  /app/order/status
```

### Çözülen Temel Problemler

**Just-In-Time (JIT) Sipariş Senkronizasyonu**
Sipariş listesi her açıldığında ERP'ye toplu sorgu atılmaz. Sadece `Pending` durumundaki kayıtların Sysmond ID'leri toplanıp tek bir toplu istekle (`Ids=...`) sorgulanır; ERP tarafında değişen statüler (`Invoiced`, `Canceled`) anında yerel veritabanına yansıtılır. Bu yaklaşım gereksiz API trafiğini büyük ölçüde azaltır.

**Çift Yönlü Sipariş Eşitleme (Two-Way Sync)**
Sysmond panelinden manuel girilen siparişler, kalemleriyle birlikte otomatik olarak yerel veritabanına aktarılır. ERP tarafında tamamen silinen taslak siparişler ise yerel veritabanından da temizlenir (**orphan cleanup**) — iki sistem arasında "hayalet kayıt" birikmesi engellenir.

**Tek Adımlı Atomik Faturalama**
`CreateAndConfirmInvoiceAsync` metodu, satıcı faturayı onayladığında tek bir işlem zincirinde:
1. ERP'deki siparişi `Approved (20)` statüsüne çeker,
2. Siparişe bağlı (`orderDocRefs`) bir giden fatura taslağı açar,
3. `ISysmondStockService` üzerinden ürünün dinamik fiyat listesini (`stockPriceId`), ölçü birimini (`measureUnitId`) ve KDV oranını çözerek kalemleri faturaya işler,
4. İşlem başarılı olursa yerel siparişi `Invoiced`, faturayı `Confirmed` yapar.

Bu akışın tek metotta, adım adım ve hataya karşı kontrollü şekilde yürütülmesi, ERP ile yerel sistem arasında tutarsız ("faturası kesilmiş ama sipariş güncellenmemiş" gibi) durumların oluşmasını engeller.

**State Machine ile Durum Yönetimi**
- Kargo çıkışı yapıldığında (`Shipped`) ERP tarafı otomatik olarak `PartiallyDelivered (30)` durumuna çekilir.
- Sipariş iptalinde ERP'ye `Cancelled (-100)` statüsü iletilir ve rezerve edilmiş stoklar `MovementType.Entry` logu ile depoya iade edilir.

---

## 🛠️ Teknoloji Yığını

| Alan | Teknoloji |
|---|---|
| Framework & Dil | .NET 10 (C# 13) |
| Veritabanı & ORM | Microsoft SQL Server, Entity Framework Core 10 |
| Güvenlik & Yetki | JWT Bearer Tokens, Custom Policy-Based Authorization (RBAC + PBAC) |
| Önbellek & Sınırlama | Redis / `IDistributedCache`, ASP.NET Core Rate Limiting |
| Harici Entegrasyon | Sysmond AX REST Integration Client, Typed HttpClient'lar |
| Loglama & Eşleme | Serilog (structured logging), AutoMapper |
| Test & Mocking | xUnit, Moq, FluentAssertions |

---

## 🧪 Test Kapsamı

Servis katmanındaki tüm dış ERP bağımlılıkları (`ISysmondOrderService`, `ISysmondInvoiceService`, `ISysmondStockService`, `ISysmondWarehouseService`) Moq ile mock'lanarak izole biçimde test edilmiştir:

```bash
dotnet test
```

```
Passed!  - Failed: 0, Passed: 19, Skipped: 0, Total: 19, Duration: 0.6s
```

**Kapsanan senaryolar:** RBAC/PBAC izin doğrulama, stok artış/azalış audit logları, tek adımlı faturalama entegrasyon zinciri, JIT ERP statü filtreleme ve JWT token üretim süreçleri.

---

## ⚙️ Kurulum ve Çalıştırma

```bash
# 1. Projeyi klonlayın
git clone https://github.com/kullaniciadi/ECommerceManagement.git
cd ECommerceManagement

# 2. Veritabanını güncelleyin
dotnet ef database update --project src/ECommerceManagement.Repository --startup-project src/ECommerceManagement.Api

# 3. API'yi çalıştırın
dotnet run --project src/ECommerceManagement.Api
```

API ayağa kalktıktan sonra Swagger UI üzerinden test edebilirsiniz:

```
http://localhost:5275/swagger
```
