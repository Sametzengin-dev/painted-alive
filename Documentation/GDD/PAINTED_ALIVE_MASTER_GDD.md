# PAINTED ALIVE — MASTER GAME DESIGN DOCUMENT

> **Belge durumu:** Ana tasarım kaynağı / yaşayan doküman  
> **Sürüm:** 0.2.0 — Esnek Atölye oyuncu yapısı revizyonu  
> **Son güncelleme:** 15 Temmuz 2026  
> **Belge sahibi:** Samet  
> **Hedef motor:** Unity 6 LTS  
> **Ana platform:** Windows PC / Steam  
> **Ana yapı:** 1–4 Ressam ve 1–6 Figür destekleyen esnek asimetrik multiplayer  
> **Önerilen arkadaş formatı:** 2 Ressam – 4 Figür  
> **Rekabetçi format:** 4v4 Frame Versus  
> **Çalışma adı:** PAINTED ALIVE

---

## 0. BU BELGE NASIL KULLANILMALI?

Bu dosya, PAINTED ALIVE hakkında oluşturulacak bütün tasarım, kod, sanat, ses, pazarlama ve üretim kararlarının ana referansıdır. Yeni bir ChatGPT/Codex konuşmasında, ekip toplantısında veya dış kaynak briefinde bu belge temel alınmalıdır.

### 0.1 Karar etiketleri

Belgedeki maddeler aşağıdaki statülerden biriyle yorumlanır:

- **[KİLİTLİ]**: Oyunun kimliğini oluşturan değişmez karar. Açıkça revizyon kararı alınmadan değiştirilmez.
- **[HEDEF]**: Ulaşılmak istenen tasarım sonucu. Teknik veya test kaynaklı revizyon yapılabilir.
- **[HİPOTEZ]**: Prototip ve oyuncu testiyle doğrulanması gereken fikir.
- **[SONRA]**: İlk sürüm/MVP kapsamına girmeyen, gelecek geliştirme olasılığı.
- **[YASAK]**: Oyunun odağını, dengesini veya üretilebilirliğini bozduğu için yapılmaması gereken yaklaşım.

### 0.2 Yeni konuşmalarda kullanılacak talimat

> `PAINTED_ALIVE_MASTER_GDD.md dosyasını ana ve bağlayıcı kaynak kabul et. [KİLİTLİ] kararlarla çelişme. Yeni önerileri mevcut sistemlerle uyumluluk, oyuncu karşı hamlesi, multiplayer adaleti, teknik uygulanabilirlik ve indie kapsam açısından değerlendir. [HİPOTEZ] kararları kesinleşmiş gibi sunma.`

### 0.3 Değişiklik yönetimi

Bu dosyada önemli bir karar değiştirildiğinde:

1. Değişen madde açıkça belirtilir.
2. Değişikliğin nedeni yazılır.
3. Etkilenen sistemler listelenir.
4. Yeni sürüm numarası verilir.
5. Eski karar sessizce silinmek yerine değişiklik günlüğüne eklenir.

---

# 1. OYUNUN ÖZÜ

## 1.1 Tek cümlelik yüksek konsept

**[KİLİTLİ]**

> Bir grup oyuncu, içinde hapsoldukları devasa canlı bir tablodan kaçmaya çalışırken karşılarındaki Ressamlar tabloyu dışarıdan gerçek zamanlı olarak boyar; çizdikleri yollar, yaratıklar, tuzaklar ve hava olayları içeride fiziksel olarak gerçek olur. Lobinin seçtiği Figür ve Ressam sayısı değişebilir, fakat iki taraf arasındaki yaratıcı karşı oyun değişmez.

## 1.2 Pazarlama cümlesi

**[KİLİTLİ]**

> **They are not playing the level. They are painting it.**  
> **Onlar bölümü oynamıyor. Bölümü boyuyor.**

## 1.3 Oyuncunun ilk beş saniyede anlaması gereken şey

Bir tanıtım videosunu sessiz izleyen kişi bile şunları anlayabilmelidir:

1. Tablonun içinde küçük oyuncular kaçmaktadır.
2. Tablonun dışındaki dev fırçalar dünyaya müdahale etmektedir.
3. Çizilen boya yalnızca görsel renk değil, fiziksel zemin veya canlıya dönüşmektedir.
4. Figürler pasif kurban değildir; çizimleri kesebilir, emebilir, sabitleyebilir ve kendi lehlerine kullanabilir.
5. Bu bir tek oyunculu yaratım aracı değil, iki insan takımı arasındaki canlı rekabettir.

## 1.4 Tür

- Esnek oyuncu sayılı asimetrik multiplayer
- Özel lobilerde 1–4 Ressam ve 1–6 Figür
- Resmî rekabetçi formatta 4v4 rol değişimli Versus
- Rekabetçi aksiyon/macera
- Fizik tabanlı takım oyunu
- Traversal ve kaçış
- Sistemik sandbox etkileşimleri
- Sosyal/viral “friendslop” enerjisi taşıyan, fakat ustalık derinliği bulunan oyun

## 1.5 Temel referanslardan alınan değerler

PAINTED ALIVE hiçbir referans oyunun yüzeyini kopyalamaz. Yalnızca şu tasarım değerlerini hedefler:

| İlham alanı | Alınan değer | Kopyalanmayacak yüzey |
|---|---|---|
| Left 4 Dead 2 Versus | 4 kişilik takım koordinasyonu, saldırı kombinasyonları, aynı parkurda rol değişimi, mesafe tabanlı rekabet | Zombiler, silahlı hayatta kalanlar, özel enfekte kopyaları, güvenli oda yapısı |
| PEAK | Tek parça ve hatırlanabilir yolculuk, fiziksel mücadele, arkadaş kurtarma, başarısızlığın doğal hikâye üretmesi | Dağcılık teması, kamp/izci kimliği, birebir dayanıklılık sistemi |
| Meccha Chameleon | Tek cümlede anlatılabilen mekanik, düşük giriş bariyeri, sosyal kaos, paylaşılabilir anlar | Kamuflajla saklambaç, vücut boyama, Prop Hunt benzeri yapı |

---

# 2. TASARIM SÜTUNLARI

## 2.1 Dünya, rakip takımın silahıdır

**[KİLİTLİ]** Ressamlar Figürleri klasik silahlarla vurmaz. Ressamların ana eylemi dünyanın malzemesini, rotasını ve davranışını değiştirmektir.

Ressam saldırısı şu soruya cevap vermelidir:

> “Bu oyuncuyu nasıl hasarlayabilirim?” değil, “Bu takımı istediğim kötü karara nasıl zorlayabilirim?”

## 2.2 Her müdahalenin karşı hamlesi vardır

**[KİLİTLİ]** Figürlerin üzerine anında, kaçınılmaz ve karşılıksız ölüm bırakılamaz.

Her Ressam eylemi:

- Önceden okunabilir bir işaret verir.
- En az bir kaçış yöntemi içerir.
- En az bir Figür aracıyla bozulabilir.
- Yanlış kullanılırsa Figürlere avantaj sağlayabilir.
- Takım arkadaşının boyasıyla istenmeyen etkileşime girebilir.

## 2.3 İki taraf da yaratıcıdır

**[KİLİTLİ]** Yaratıcılık yalnızca Ressamlara ait değildir.

- Ressamlar dünyayı oluşturur ve bozar.
- Figürler o dünyayı yeniden yorumlar, keser, taşır, kurutur ve silaha çevirir.

En iyi anlar, Ressamların yaptığı şeyin Figürler tarafından beklenmedik biçimde kullanılmasından doğmalıdır.

## 2.4 Hata komik olmalı, kontrol rastlantısal olmamalı

**[KİLİTLİ]** Fizik hafif kaotik olabilir; kontrol gecikmeli, keyfî veya güvenilmez olmamalıdır.

Oyuncu başarısız olduğunda nedenini anlayabilmelidir. Komedi fizik kurallarının birleşiminden doğmalı, oyunun rastgele kontrolü bozmasından değil.

## 2.5 Yolculuk hissi korunmalıdır

**[KİLİTLİ]** Ana deneyim arena tabanlı round’lardan ibaret değildir. Figürler tablonun başlangıcından çerçeve çıkışına kesintisiz ilerler.

- Başlangıç ve hedef ilişkilidir.
- Geride bırakılan bölgeler görülebilir veya hatırlanabilir.
- Çıkış çoğu bölgede uzaktan seçilebilir.
- Harita, olayların geçtiği pasif fon değil, maçın hikâyesidir.

## 2.6 Okunabilirlik görsel gösteriden önce gelir

Her malzemenin uzaktan tanınan davranış dili olmalıdır:

- Yağlı boya: kalın, parlak, fiziksel, yavaş.
- Mürekkep: keskin, siyah, canlı, hızlı.
- Suluboya: şeffaf, akışkan, yayılan.
- Silgi/restorasyon: lifleri açığa çıkaran, katman kaldıran.
- Boş tuval: krem, kuru, tehlikeli ve savunmasız.

## 2.7 Kolay başla, yıllarca ustalaş

Yeni Ressam çizgi çekerek duvar yapabilmelidir. Deneyimli Ressam:

- Kuruma zamanını hesaplar.
- Rakibin rota tercihlerini okur.
- Diğer malzemelerle kombinasyon kurar.
- Figür karşı araçlarını bait’ler.
- Aynı saldırıyı iki farklı amaca dönüştürür.

---

# 3. HEDEF KİTLE VE ÜRÜN KONUMLANDIRMASI

## 3.1 Birincil hedef kitle

- 2–10 kişilik arkadaş grupları; özellikle 4–6 kişilik düzenli gruplar
- Rekabeti seven co-op oyuncuları
- L4D2 Versus, PEAK, Lethal Company, R.E.P.O. ve sosyal fizik oyunlarını sevenler
- Yayıncılar ve kısa video üreticileri
- Kolay anlaşılır fakat yüksek ustalık tavanı arayan oyuncular

## 3.2 İkincil hedef kitle

- Görsel sanat ve yaratım temalı oyunları sevenler
- Speedrun ve günlük seed toplulukları
- Özel lobi/parti oyunu arayanlar
- İzlemesi oynaması kadar anlaşılır rekabet oyunlarını takip edenler

## 3.3 Duygusal hedef

Bir maç şu duyguların ritmini üretmelidir:

1. Merak: “Rakip ne çizecek?”
2. Okuma: “Fırça gölgesi hangi saldırıya ait?”
3. Panik: “Bu boya kurumadan geçmeliyiz.”
4. Takım koordinasyonu: “Süngeri hazırla, ben duvarı keseceğim.”
5. Beklenmedik çözüm: “Rakibin duvarını rampa yaptık.”
6. Kahkaha: “Kendi yaratıkları onları engelledi.”
7. Ustalık: “Bu komboyu tam burada kurmak istediler ve bozduk.”

## 3.4 Ürün ölçeği

**[HEDEF]** İlk ticari sürüm, içerik miktarıyla değil sistemlerin derinliğiyle değer üretir.

- Tek güçlü ana harita/kampanya kabul edilebilir.
- Dört kusursuz Ressam rolü, sekiz vasat rolden değerlidir.
- On iki çok etkileşimli araç, elli tek amaçlı eşyadan değerlidir.
- Stilize ve okunabilir grafik, pahalı fotogerçekçilikten değerlidir.

---

# 4. ESNEK ATÖLYE VE MAÇ YAPISI

## 4.1 Oyunun kimliği

**[KİLİTLİ]** PAINTED ALIVE’ın değişmez kimliği belirli bir oyuncu sayısı değil, **tablonun içinde ilerleyen Figürler ile tabloyu dışarıdan değiştiren Ressamlar arasındaki asimetridir**.

- Figür sayısı: 1–6
- Ressam sayısı: 1–4
- Teknik hedef: Aynı oturumda en fazla 10 aktif oyuncu
- Özel lobide takım kompozisyonu lobi sahibi tarafından seçilir.
- Resmî matchmaking yalnızca test edilmiş preset’leri kullanır.
- Oyuncu sayısı değişse bile temel boya davranışları ve karşı hamle kuralları değişmez.

## 4.2 Desteklenen maç aileleri

### Expedition / Tek yolculuk

Bir grup Figür bir grup Ressama karşı tek kez tablodan kaçmaya çalışır.

- Eşit takım sayısı gerektirmez.
- Rol değişimi zorunlu değildir.
- Arkadaş lobileri, hızlı oyun ve deneysel kompozisyonlar için uygundur.
- Sonuç “hangi takım daha iyi?” yerine yolculuk skoru, çıkabilen Figür sayısı ve lobi hedefleriyle değerlendirilir.

### Rotation / Dönen Sergi

Toplam arkadaş grubu içinden belirli sayıda oyuncu her tur Ressam koltuğuna oturur.

- Örneğin altı oyuncuda her tur 2 Ressam ve 4 Figür bulunur.
- Üç tur sonunda her oyuncu bir kez Ressam olur.
- Tur seed’i ve temel kaynak dağılımı korunur.
- Bireysel Figür skoru, Ressam olarak rakipte oluşturulan skor kaybı ve takım katkısı birlikte değerlendirilir.
- Parti içi rekabet için güçlüdür; ilk sürümde puan formülü test gerektirir.

### Frame Versus / Aynalı rekabet

İki eşit takım aynı koşullarda sırayla Figür ve Ressam olur.

**[KİLİTLİ]** Resmî rekabetçi format iki yarılı 4v4 Frame Versus’tur.

İlk yarı:

- Takım A: Figürler
- Takım B: Ressamlar

İkinci yarı:

- Takım B: Figürler
- Takım A: Ressamlar

Aynı temel harita seed’i, çevresel olay takvimi, eşya dağılımı ve çıkış koşulları kullanılır. İlk yarıdaki kullanıcı çizimleri ikinci yarıya taşınmaz.

## 4.3 Önerilen preset’ler

| Preset | Kompozisyon | Amaç | Rol değişimi |
|---|---:|---|---|
| Sketch Duel | 1 Ressam – 1 Figür | Eğitim, düello, mekanik ustalık | İsteğe bağlı |
| Small Canvas | 1 Ressam – 2 Figür | Küçük arkadaş grubu | İsteğe bağlı |
| Atelier Escape | 2 Ressam – 4 Figür | Önerilen sosyal/arkadaş deneyimi | Tek koşu veya rotasyon |
| Frame Versus | 4v4 | Ana rekabetçi ve ranked format | Zorunlu |
| Gallery Rush | 2 Ressam – 6 Figür | Kalabalık parti ve kaos | Tek koşu |
| Grand Exhibition | 4 Ressam – 6 Figür | Özel lobi/stres formatı | İsteğe bağlı |

Preset adları ve halka açık kuyruk sayısı oyuncu testleriyle doğrulanacaktır. Aynı anda çok sayıda genel matchmaking kuyruğu açılmaz.

## 4.4 Lobi kontrolü

Özel lobi sahibi şunları seçebilir:

- Figür ve Ressam koltuk sayısı
- Expedition, Rotation veya Frame Versus formatı
- Rol seçimi, rastgele rol veya lobi oylaması
- Aynı Ressam disiplininin tekrar seçilip seçilemeyeceği
- Boş koltukların botla doldurulması
- Maç süresi ve bölge sayısı
- Pigment ölçekleme profili
- Rekabetçi eşitlemenin açık/kapalı olması
- Devam eden maça katılma ve seyirci izni

**[YASAK]** Özel lobi özgürlüğü gerekçe gösterilerek bütün olası oyuncu oranlarının ranked seviyesinde dengeli olduğu iddia edilmez. Oyun, desteklenen her kompozisyonda çalışır; yalnızca resmî preset’ler rekabetçi adalet garantisi alır.

## 4.5 Değişken Ressam sayısında disiplinler

Dört malzeme birer zorunlu oyuncu slotu değildir. Ressam sayısı azaldığında oyuncular **Palet Yükü** sistemiyle birden fazla disipline erişebilir.

- 4 Ressam: Her oyuncu bir ana disiplin taşır.
- 3 Ressam: İki oyuncu tek disiplin, bir oyuncu ana + sınırlı ikincil disiplin taşır.
- 2 Ressam: Her oyuncu bir ana ve bir ikincil disiplin taşır.
- 1 Ressam: Oyuncu iki aktif disiplin seçer; Çerçeve Duraklarında diğer disiplinlerle değiştirebilir.

İkincil disiplin:

- Ana disiplin kadar geniş yetenek setine sahip değildir.
- Aynı pigment rezervi üzerinde baskı oluşturur.
- Rolün güçlü nihai yeteneğini açmaz.
- Temel malzeme kombinasyonlarını mümkün kılar.

Özel lobide “Sınırlı Palet” seçeneğiyle seçilmeyen disiplinler tamamen kapatılabilir. Bu ayar çeşitlilik içindir ve ranked denge standardı değildir.

## 4.6 Atölye Bütçesi ve otomatik ölçekleme

**[KİLİTLİ]** Oyuncu sayısı dengelemesi Figürlerin canını veya Ressamların doğrudan hasarını kaba biçimde çarpmakla yapılmaz. Sistem, Ressamların aynı anda kontrol edebildiği alanı ve eylem sıklığını düzenler.

Atölye Bütçesi aşağıdaki girdilerden hesaplanır:

```text
AtölyeBütçesi =
    FigürSayısı
  × BölgeBaskıKatsayısı
  × SeçilenZorlukProfili
  ÷ max(1, RessamSayısı)
```

Bu değer doğrudan tek bir güç çarpanı değildir; aşağıdaki sınırlara dağıtılır:

- Ressam başına pigment yenilenmesi
- Aktif stroke ve yaratık sınırı
- Büyük eylem cooldown’ı
- Ortak Kompozisyon Gerilimi
- Boyanın kuruma/dağılma penceresi
- Harita üzerindeki eşzamanlı baskı bölgesi sayısı
- Figür aracı ve sarf malzemesi miktarı
- Curator’ın doğal tehdit yoğunluğu

Daha az Ressam, kişi başına daha fazla araca erişir; fakat aynı anda her yerde bulunamaz. Daha fazla Ressam, daha geniş alanı kontrol eder; fakat kişi başına pigment ve aktif nesne bütçesi düşer.

## 4.7 Süre hedefi

- Hazırlık: 30–45 saniye
- Expedition koşusu: 10–18 dakika
- Frame Versus yarısı: 12–18 dakika
- Rol değişimi: 45–60 saniye
- Tam Frame Versus: 25–35 dakika
- Rotation: Tur sayısına göre 20–40 dakika

**[HİPOTEZ]** Atelier Escape ilk testlerde 10–12 dakikalık tek koşu olarak denenir; yolculuk derinliği yetersizse uzatılır.

## 4.8 Figürlerin amacı

- Tablonun başlangıç bölgesinden çerçeve yırtığına ulaşmak.
- Mümkün olduğunca fazla takım üyesini çıkarmak.
- Tuval bütünlüğünü gereksiz yere bozmamak.
- İsteğe bağlı pigment özlerini toplamak.
- Kaynakları ve araçları akıllıca kullanmak.

## 4.9 Ressamların amacı

- Figürlerin ilerlemesini yavaşlatmak.
- Takımı ayırmak.
- Figür araçlarını tükettirmek.
- Figürleri leke durumuna sokmak.
- Tuvali tamamen yok etmeden rotayı kontrol etmek.
- Koordineli saldırılarla mümkün olan en düşük Figür skorunu oluşturmak.

## 4.10 Maç sonu

- Expedition’da Figürlerin yolculuk skoru, çıkan oyuncu oranı ve lobi hedefi gösterilir.
- Rotation’da her turun Figür ve Ressam performansı ortak tabloda birikir.
- Frame Versus’ta iki yarının sonunda takımların Figür olarak topladığı normalize skor karşılaştırılır.
- Ressam tarafında ayrı “öldürme puanı” yoktur; Ressam başarısı Figürlerin ilerlemesini kurallı karşı oyunla azaltması üzerinden ölçülür.
- Özel lobide skor kapatılabilir; yalnızca yolculuk özeti ve maç hikâyeleri gösterilebilir.

---

# 5. MAÇIN RİTMİ

## 5.1 Hazırlık evresi

Figürler:

- Ana araç seçer.
- İki sarf malzemesi seçer.
- İlk rota hakkında konuşur.
- Takım rolleri belirler: öncü, kurtarıcı, malzeme taşıyıcı, rota kırıcı.

Ressamlar:

- Rollerini kesinleştirir.
- İlk bölgenin potansiyel darboğazlarını görür.
- Boya kombinasyonu planlar.
- Çizim yapamaz veya tuzak yerleştiremez.

## 5.2 Baskı ve nefes döngüsü

Her bölge şu ritmi üretmelidir:

1. Okuma alanı: Figürler rotayı inceler.
2. İlk hafif müdahale: Ressamlar kaynak test eder.
3. Araç karşılığı: Figürler hangi araçları harcayacağını seçer.
4. Kombinasyon saldırısı: Ressamlar mevcut disiplinlerini birleştirir veya tek Ressam iki malzemeyi art arda kullanır.
5. Kurtarma/kaçış: Figürler takım koordinasyonuyla çıkar.
6. Kısa nefes: Kaynak toplama veya rota seçimi.
7. Yeni malzeme kuralı: Bir sonraki bölgenin ana fikri açılır.

Oyuncu sürekli maksimum baskı altında bırakılmaz. Sessizlik, yaklaşan fırçanın gölgesini daha korkutucu yapar.

## 5.3 Çerçeve durakları

**[HEDEF]** Büyük bölgeler arasında 20–30 saniyelik kısa duraklar bulunur.

Figürler burada:

- Leke olmuş takım arkadaşını kısmen restore eder.
- Bir eşya seçer.
- Araç dayanıklılığını sınırlı ölçüde yeniler.
- İlerideki bölgeyi görür.

Ressamlar burada:

- Pigmentlerinin bir kısmını yeniler.
- Rol geliştirmesi seçer.
- Takım içi rol değişimi yapabilir.
- Sonraki bölümün kompozisyonunu inceler.

Durak tamamen güvenli değildir; zaman baskısı veya hafif çevresel hareket devam eder.

---

# 6. DÜNYA VE ANLATI ÇERÇEVESİ

## 6.1 Anlatının görevi

**[KİLİTLİ]** Hikâye ana oynanışı açıklamalı ve atmosferi güçlendirmeli; uzun sinematiklerle maç ritmini kesmemelidir.

## 6.2 Dünya önermesi

**[HİPOTEZ]** İnsanların yarattığı bazı sanat eserleri, yeterince yoğun hatıra ve yorum biriktirdiğinde “canlı kompozisyonlara” dönüşür. Bu tablolar izleyicileri içine çekebilir, içlerindeki Figürleri dışarı çıkarabilir ve kendilerini yeniden yazabilir.

“Çerçeve Yarışları”, bu canlı eserlerin içinde gerçekleştirilen kontrollü fakat tehlikeli rekabetlerdir. Çerçeve her oturumda bazı katılımcıları Figür, bazılarını El/Ressam olarak seçer. Katılımcı sayısı ve roller serginin kuralına göre değişebilir. Rekabetçi sergilerde roller tersine döner; sosyal sergilerde aynı grup tek bir yolculuğu tamamlayabilir. Böylece tablo farklı büyüklükteki insan gruplarının niyetini besler.

## 6.3 Figür kimliği

Figürler doğrudan gerçek dünyadan küçültülmüş insanlar olmak zorunda değildir. Tablonun içinde yeniden çizilmiş oyuncu avatarlarıdır. Bu nedenle:

- Boyayla deforme olabilirler.
- Leke hâline gelebilirler.
- Restore edilebilirler.
- Farklı sanat stillerine uyarlanabilirler.
- Şiddet grafik gore olmadan gösterilebilir.

## 6.4 Ressam kimliği

Ressamlar ekranda tam insan bedeniyle görünmez. Oyuncu kendi:

- Eldivenini
- Fırça sapını
- Bilek işaretlerini
- Masadaki kişisel araçlarını

görür. Takım arkadaşları dev araçları ve renk işaretleriyle temsil edilir.

## 6.5 Ton

- Sürreal macera
- Dokunsal ve hafif tekinsiz sanat dünyası
- Kara mizah olmadan fiziksel komedi
- Çocuksu olmayan renkli estetik
- Ciddi rekabet ile absürt olayların dengesi

**[YASAK]** Aşırı korku/gore, okul öncesi çocuk oyunu estetiği, sıradan büyülü ressam klişesi veya yalnızca “sanat güzeldir” mesajına dayalı yüzeysel anlatı.

---

# 7. KAMERA VE BAKIŞ AÇISI

## 7.1 Figür kamerası

**[KİLİTLİ]** Üçüncü şahıs kamera.

Hedefler:

- Karakterin boya durumunu vücudundan okumak.
- Takım arkadaşının elini yakalamayı kolaylaştırmak.
- Dev fırçaların ölçeğini göstermek.
- Platform ve akış rotasını okunabilir kılmak.

Kamera özellikleri:

- Omuz değiştirme.
- Dar alanlarda otomatik yakınlaşma.
- Dev fırça yaklaştığında kontrollü genişleme.
- Baş dönmesini azaltan ufuk stabilizasyonu.
- Islak yüzeyde hız hissi sağlayan ama kontrolü saklamayan hareket.

## 7.2 Ressam kamerası

**[KİLİTLİ]** Ressam oynanışı statik kuşbakışı harita editörü değildir. Dev fırçanın arkasında birinci şahıs/omuz üstü fiziksel çalışma hissi verir.

İki kesintisiz kamera durumu bulunur:

### Planlama duruşu

- Fırça tuvalden kalkar.
- Kamera hafif yükselir.
- Figürlerin bulunduğu alan ve 20–30 metre önü okunur.
- Takım arkadaşlarının araçları görünür.
- Uzak harita serbestçe gezilemez.

### Uygulama duruşu

- Fırça yüzeye yaklaşır.
- Kamera boya dokusuna iner.
- Fırça açısı, basıncı ve kalınlığı hassas yönetilir.
- Figürler daha büyük ve tepkileri daha okunur görünür.

## 7.3 Ressamın hareket sınırı

Ressam:

- Figürlerin aktif bölgesi çevresindeki çalışma rayında hareket eder.
- Belirli mesafe ileriye bakabilir.
- Çok uzaktaki finale önceden kalıcı tuzak kuramaz.
- Figürlerin hemen arkasına sınırsız müdahale edemez.
- Takım arkadaşlarının fırçalarıyla fiziksel/işlevsel alan çatışması yaşayabilir.

---

# 8. FİGÜR HAREKET SİSTEMİ

## 8.1 Temel hareketler

- Yürüme ve koşma
- Kontrollü zıplama
- Kenara tutunma
- Kısa tırmanma
- Eğimde kayma
- Islak boyada yüzme/çabalama
- İnce çizgide denge
- Halata tutunma
- Takım arkadaşının elini yakalama
- Küçük nesneleri itme/çekme
- Dev fırça kıllarına kısa süreli tutunma

## 8.2 Dayanıklılık yaklaşımı

**[HİPOTEZ]** Klasik stamina barı yerine “Tanım/Definition” göstergesi kullanılabilir. Tırmanma, ağır eşya ve boya yükü Tanım tüketir. Boyayla kirlenme maksimum Tanım değerini düşürür.

Test edilmesi gerekenler:

- Ayrı stamina sistemi gerçekten derinlik sağlıyor mu?
- Yoksa Netlik sistemi zaten yeterli baskı oluşturuyor mu?
- Hareket temposu PEAK benzeri mücadele verirken L4D2 akışını yavaşlatıyor mu?

## 8.3 Takım fizik etkileşimi

Figürler:

- Birbirinin elini yakalayabilir.
- Yerdeki oyuncuyu çekebilir.
- Leke oyuncuyu süngerde taşıyabilir.
- Ağır araçları iki kişi kullanabilir.
- Aynı dar zeminde birbirini hafifçe itebilir.

**[YASAK]** Takım arkadaşlarının sınırsız ve kasıtlı griefing ile birbirini sürekli öldürebilmesi. Fiziksel temas komik risk yaratmalı; tek tuşla troll silahı olmamalıdır.

---

# 9. FİGÜR DURUM SİSTEMİ: NETLİK VE LEKE

## 9.1 Netlik

**[KİLİTLİ]** Figürlerin klasik kan/sağlık sistemi yerine Netlik (Clarity) durumu vardır.

Netlik, karakterin çizilmiş kimliğinin ne kadar korunduğunu temsil eder.

### Netlik seviyeleri

| Seviye | Görsel durum | Oynanış etkisi |
|---|---|---|
| Temiz | Keskin kontur, temiz kıyafet | Tam hareket ve araç verimi |
| Lekeli | Bölgesel boya izleri | Hafif ağırlık veya yüzey etkisi |
| Bozulmuş | Kontur dalgalanır, renkler karışır | Düşük tutuş, yavaş araç kullanımı |
| Çözülüyor | Vücut kısmen akışkan | Kurtarma penceresi, ciddi hareket kaybı |
| Leke | Fiziksel beden yok | Alternatif destek rolü |

## 9.2 Bölgesel boya etkisi

**[HİPOTEZ]** Boyanın vücudun farklı bölgelerine bulaşması farklı sonuçlar yaratabilir:

- Bacak: hareket/kayma etkisi
- Kol: araç kullanımı
- Sırt: eşya kapasitesi
- Baş: görüş/işitsel bozulma

Bu sistem okunabilirliği veya ağ maliyetini aşırı artırırsa sadeleştirilecektir.

## 9.3 Leke hâli

Leke olan oyuncu seyirciye dönüşmez.

Yapabilecekleri:

- Duvar ve zeminde sürünmek.
- İnce çatlaklardan geçmek.
- Ressam taslaklarını takımından biraz önce görmek.
- Kısa yön işaretleri bırakmak.
- Küçük Mürekkep yaratıklarını geçici bozmak.
- Suluboya akışına kapılarak taşınmak.
- Takım arkadaşının süngerine girmek.
- Temiz bir yüzeye yayılıp kısa süreli tutunma izi oluşturmak.

Yapamayacakları:

- Tek başına tamamen restore olmak.
- Ana araç kullanmak.
- Çıkıştan normal Figür puanıyla geçmek.
- Ressam saldırılarını sınırsız işaretlemek.

## 9.4 Restore olma

Leke oyuncu şu yöntemlerle dönebilir:

- Takım arkadaşının Sünger + Temiz Pigment kombinasyonu
- Bölüm sonu restorasyon noktası
- Nadir bir Restorasyon Verniği
- Belirli harita olayları

Restore edilen oyuncu tam Netlikte değil, kısmi kaynakla döner.

---

# 10. FİGÜR ANA ARAÇLARI

Her Figür bir ana araç taşır. Takım aynı araçtan birden fazla seçebilir; ancak çeşitlilik kombinasyon avantajı sağlar.

## 10.1 Palet Bıçağı

Rol: Kesme, şekillendirme, yakın müdahale.

İşlevler:

- Yağlı boya duvarında geçit açar.
- Islak boyayı kazıyıp küçük miktarda taşır.
- Mürekkep yaratığının sembol organını keser.
- İnce tuval katmanında kontrollü yarık açar.
- Kurumuş boya kütlesini kırılabilir parçalara böler.

Sınırlamalar:

- Suluboyaya karşı zayıf.
- Büyük yapıda yavaş.
- Aşırı kullanım Tuval Bütünlüğüne zarar verebilir.

## 10.2 Sünger

Rol: Temizlik, taşıma ve akış kontrolü.

İşlevler:

- Takım arkadaşının Netliğini kısmen geri getirir.
- Suluboyayı emer.
- Küçük Mürekkep lekelerini toplar.
- Leke takım arkadaşını taşır.
- Emilen sıvıyı başka yere bırakır.

Sınırlamalar:

- Doldukça ağırlaşır.
- Yanlış boya karışımı süngeri tehlikeli hâle getirir.
- Tam doluyken darbe alırsa patlayıp alanı boyar.

## 10.3 Sabitleyici Sprey

Rol: Kuruma zamanı ve durum kilitleme.

İşlevler:

- Islak köprüyü anında sertleştirir.
- Mürekkep yaratığını kısa süreli heykele çevirir.
- Suluboya yayılımını durdurur.
- Figürü kaygan yüzeyde sabitleyebilir.
- Yağlı boyayı erken kurutarak kırılgan hâle getirir.

Risk:

- Takım arkadaşını yanlışlıkla yüzeye sabitleyebilir.
- Zamanından önce kuruyan rota kullanılamaz hâle gelebilir.

## 10.4 Çerçeve Tabancası

Rol: Kurtarma, halat ve ankraj.

İşlevler:

- Tuvale çivi atar.
- Halat rotası kurar.
- Düşen oyuncuyu yakalamaya yardım eder.
- Boya kütlesini belirli pozisyonda tutar.
- Mürekkep yaratığını yüzeye bağlar.

Risk:

- Çivi Silgi tarafından çevresinden ayrılabilir.
- Islak boya üzerindeki ankraj kayabilir.
- Halatlar başka boya kütlelerine dolanabilir.

## 10.5 Rulo

Rol: Geniş yüzey onarımı ve temel yol üretimi.

İşlevler:

- Küçük boşlukları düz pigmentle kapatır.
- Ressam çiziminin üstünü geçici olarak örter.
- Leke oyuncuya restore yüzeyi sağlar.
- Yüzey rengini değiştirerek bazı malzeme etkilerini azaltır.

Sınırlamalar:

- Çok pigment tüketir.
- İnce ve savunmasız yol üretir.
- Taşırken hareketi yavaşlatır.

## 10.6 Büyüteç / Perspektif Lensi

Rol: Perspektif katmanlarıyla oynama.

İşlevler:

- Küçük resim nesnesini geçici büyütür.
- Uzak katmandaki platformu kullanılabilir hâle getirir.
- Mürekkep yaratığını küçültür.
- Gizli eskiz çizgilerini gösterir.

**[HİPOTEZ]** Bu araç ilk MVP’de değil, perspektif haritası vertical slice’ında değerlendirilecektir.

---

# 11. FİGÜR SARF MALZEMELERİ

- Tiner kapsülü: Küçük alandaki boyayı çözer; güvenli zemini de yok edebilir.
- Temiz pigment özü: Netlik onarımı veya boş tuvalde kısa yol.
- Vernik plakası: Küçük bir alanı Ressam müdahalesine karşı birkaç saniye kilitler.
- Kâğıt bant: Yırtığı geçici kapatır, ağır yükte kopar.
- Karbon kopya: Yakındaki bir Ressam çizgisinin zayıf kopyasını Figür lehine üretir.
- Kurutma bezi: Islak yüzeyi hızlı kurutur; tek kullanımlık.
- Çözücü iğne: Mürekkep yaratığının bir sembolünü devre dışı bırakır.
- Cep çerçevesi: Leke oyuncuyu kısa süreli sabit Figür siluetine dönüştürür.

**[HEDEF]** İlk ticari sürümde 8–12 yüksek etkileşimli sarf malzemesi yeterlidir.

---

# 12. RESSAM ORTAK KURALLARI

## 12.1 Ressam bir nişancı değildir

**[KİLİTLİ]** Fırçayla doğrudan karaktere tekrar tekrar vurmak ana oynanış değildir. Fiziksel fırça teması nadir, telegraph’lı ve çevresel sonuçlu olabilir.

## 12.2 Fırça darbesinin yaşam döngüsü

1. Hedef yüzey seçilir.
2. Soluk ön izleme görünür.
3. Fırça gölgesi Figürlere düşer.
4. Fırça fiziksel olarak yaklaşır.
5. Boya uygulanır.
6. Malzeme ıslak/aktif evreye geçer.
7. Zamanla kurur, yayılır veya canlanır.
8. Figürler ve diğer Ressamlar malzemeyi değiştirir.
9. Kalıcıysa düşük maliyetli duruma dönüşür.

## 12.3 Pigment ekonomisi

Her Ressamın kendi pigment kaynağı vardır.

Pigment:

- Zamanla yavaş yenilenir.
- Bölüm geçişinde kısmen dolar.
- Hassas ve etkili hamlelerle verim bonusu kazanabilir.
- Boşa çizimde geri verilmez.
- Aynı alana spam yapılmasını engeller.

**[HİPOTEZ]** Ressamların yalnızca hasar/engelleme başarısıyla pigment kazanması snowball yaratabilir. Ana yenilenme zaman tabanlı, performans bonusu küçük olmalıdır.

## 12.4 Çizim yasak bölgeleri

- Figürün vücut hacminin doğrudan içinde çizim başlatılamaz.
- Başlangıç ve restore alanları kısa süre korumalıdır.
- Çıkışın kendisi tamamen kalıcı kapatılamaz.
- Oyuncunun hareket etmeden sürekli hapsedileceği sıfır karşı oyunlu kutu oluşturulamaz.
- Ressamın aktif kamera bölgesi dışına kalıcı saldırı çizilemez.

## 12.5 Fırça çarpışması ve takım kaosu

**[HEDEF]** Takım arkadaşlarının araçları birbirini kısmen etkiler:

- Fırçalar aynı fiziksel hacimde yer kaplar.
- Suluboya fırçası Yağlı Boya izini bozabilir.
- Silgi takım arkadaşının çizimini kaldırabilir.
- Mürekkep yaş yüzeyde kontrolsüz yayılabilir.

Bu etkileşim griefing değil koordinasyon baskısı üretmelidir.

---

# 13. RESSAM ROLÜ 1 — YAĞLI BOYA / MİMAR

## 13.1 Rol kimliği

Fiziksel geometri, rota kontrolü ve alan bölme.

## 13.2 Temel eylem: Kalın Çizgi

Fırçanın spline hareketi boyunca kabaran bir boya mesh’i üretir.

Değişkenler:

- Kalınlık
- Fırça basıncı
- Pigment miktarı
- Çizgi hızı
- Kuruma süresi
- Yüzey açısı

Hızlı çizgi:

- Ucuz
- Hızlı
- İnce
- Kırılgan

Yavaş çizgi:

- Pahalı
- İyi telegraph
- Kalın
- Dayanıklı

## 13.3 Yetenekler

### Boya Tepesi

Bir noktada büyüyen kütle. Patlatılabilir, yuvarlanabilir veya platform olarak kullanılabilir.

### Impasto Duvarı

Kısa fakat yüksek bariyer. Takımı ayırmada güçlü, pigment maliyeti yüksektir.

### Kabartma Rampası

Eğimli fiziksel yol üretir. Rakibi belirli rotaya zorlar; yanlış kullanımda kestirme olur.

### Yeniden Yoğurma

Kurumuş kendi boya parçasını sınırlı mesafede yeniden şekillendirir.

### Masterstroke — Büyük Hamle

Geniş bir fırça darbesi alanı yükseltir veya süpürür. Uzun telegraph ve bölüm başına sınırlı kullanım.

## 13.4 Karşı oyun

- Palet Bıçağı ile kesme
- Sabitleyici ile erken kurutup kırılganlaştırma
- Tiner ile çözme
- Süngerle ıslak pigmenti azaltma
- Fırçanın yaklaşma gölgesinden kaçma
- Boyayı rampa veya siper olarak kullanma

---

# 14. RESSAM ROLÜ 2 — MÜREKKEP / CANLANDIRICI

## 14.1 Rol kimliği

Yaşayan çizimler, takip, eşya çalma ve doğrudan kontrollü pusu.

## 14.2 Sembol grameri

Mürekkep yaratığı oyuncunun çizim yeteneğine bağlı olmamalıdır. Basit semboller modüler davranış üretir.

| Sembol | Davranış |
|---|---|
| Göz | Görüş ve hedef takibi |
| Kulak | Ses/araç kullanımı takibi |
| Ayak | Kara hareketi |
| Kanat | Uçuş/kısa sıçrama |
| El | Yakalama ve taşıma |
| Ağız | Eşya yutma veya ısırma |
| Kabuk | Dayanıklılık |
| Kuyruk | Savurma/denge bozma |
| Kök | Yüzeye sabitlenme |
| Kesik çizgi | Kısa görünmezlik/ara verme |

## 14.3 Yaratık bütçesi

- Her sembol pigment ve “karmaşıklık puanı” tüketir.
- Daha çok sembol daha güçlü ama daha büyük ve okunabilir yaratık üretir.
- Aynı anda sahadaki toplam karmaşıklık sınırlıdır.
- Küçük yaratıklar AI’a bırakılabilir.
- Büyük yaratık doğrudan kontrol için uygundur.

## 14.4 Örnek yaratıklar

### Lekebacak

Ayak + Göz. Hızlı takipçi, düşük dayanıklılık.

### Çerçeve Gözcüsü

Kanat + Göz + Kulak. Konum açığa çıkarır, düşük doğrudan tehdit.

### Boya Hırsızı

El + Ağız + Ayak. Figür aracını kapıp güvenli olmayan yere taşır.

### Mürekkep Çobanı

İki El + Kuyruk. Oyuncuları belirli alana iter/sürükler.

### Duvar Ağzı

Ağız + Kök + Göz. Pusu kapanı, hareket edemez.

## 14.5 Kontrol modları

### Otonom

Yaratık basit davranış ağacıyla çalışır; Mürekkep oyuncusu yeni çizimler yapabilir.

### Sahiplenme

Oyuncu yaratığın kamerasına geçer ve doğrudan kontrol eder. Bu sırada yeni çizim yapamaz.

Sahiplenme:

- Daha yüksek beceri tavanı sağlar.
- L4D2 benzeri pusu ve zamanlama hissini özgün sistemle yakalar.
- Takım saldırısının aktif parçası olur.

## 14.6 Parçalı hasar

Yaratığın tek sağlık barı yerine sembolleri hedeflenir:

- Göz kesilirse körleşir.
- Ayak silinirse sürünür.
- El kesilirse yakalayamaz.
- Kabuk çözülürse savunmasız olur.
- Ağız süngerle tıkanabilir.

---

# 15. RESSAM ROLÜ 3 — SULUBOYA / AKIŞ YÖNETİCİSİ

## 15.1 Rol kimliği

Alan durumu, akış, kayganlık, sis ve diğer boyaların davranışını değiştirme.

## 15.2 Temel eylem: Yıkama

Şeffaf boya yüzeye bırakılır ve önceden hesaplanan eğim/akış alanında yayılır.

Etkiler:

- Zemini kayganlaştırır.
- Islak Yağlı Boyayı hareket ettirir.
- Mürekkebi dağıtır veya büyütür.
- Tuval liflerini yumuşatır.
- Figürlerin izini görünür yapar.
- Küçük nesneleri taşır.

## 15.3 Yetenekler

### Akış Kanalı

Küçük fırça darbeleriyle ana akım yönünü değiştirir.

### Sis Yıkaması

İki takımın da görüş kesinliğini azaltır. Ressamlar da tam bilgi kaybeder.

### Pigment Sıcaklığı

- Soğuk yıkama Yağlı Boyayı hızlı kurutur.
- Sıcak yıkama kurumuş boyayı yumuşatır.
- Koyu yıkama Mürekkebi gizler.
- Açık yıkama eskiz ve tuzak izlerini açığa çıkarır.

### Tuval Seli — Büyük Hamle

Geniş bir akış Figürleri, eşyaları ve çizimleri taşır. Yanlış yönde kullanılırsa Figürleri çıkışa hızlandırabilir.

## 15.4 Karşı oyun

- Süngerle emme
- Sabitleyiciyle akışı kesme
- Ankraj ve halat
- Yüksek zemine çıkma
- Akışı Mürekkep yaratıklarına yönlendirme
- Emilen suyu başka bölgeye boşaltma

---

# 16. RESSAM ROLÜ 4 — SİLGİ / RESTORATÖR

## 16.1 Rol kimliği

Katman kaldırma, geri getirme, perspektif ve takım hatası düzeltme.

## 16.2 Yetenekler

### Hassas Silme

Küçük boya veya yüzey parçasını kaldırır. Düşük bütünlük maliyeti.

### Geniş Silgi

Büyük alanı temizler. Yüksek Tuval Bütünlüğü maliyeti ve uzun telegraph.

### Geri Yükleme

Temel tablonun silinmiş bölümünü eskiz veya orijinal katman olarak geri getirir.

### Perspektif Düzeltme

Belirlenmiş perspektif nesnelerinin resim ölçeğini/katmanını değiştirir. Serbest dünyadaki her nesneye uygulanmaz.

### Beyazlatma — Büyük Hamle

Bölgeyi kısa süre boş tuvale çevirir, sonra temel eskiz olarak geri getirir. Kendi takımının bütün çizimleri de etkilenir.

## 16.3 Tuval bütünlüğü sorumluluğu

Silgi oyuncusu takımın en yüksek yıkım kapasitesine ve en önemli onarım görevine sahiptir. İyi Silgi:

- Figür rotasını kapatır.
- Takım arkadaşının hatalı duvarını kaldırır.
- Tehlikeli yırtığı restore eder.
- Bütünlüğü sıfırlamadan baskı kurar.

---

# 17. BOYA ETKİLEŞİM MATRİSİ

## 17.1 Temel kombinasyonlar

| Kaynak | Hedef | Sonuç | Olası karşı kullanım |
|---|---|---|---|
| Suluboya | Islak Yağlı Boya | Boya şekli kayar ve incelir | Figür duvarı rampa yapar |
| Suluboya | Kuru Yağlı Boya | Yüzey kayganlaşır | Kayarak hız kazanılır |
| Suluboya | Mürekkep | Mürekkep yayılır/büyür, kontrol düşer | Yaratığın sembolleri ayrışır |
| Sabitleyici | Yağlı Boya | Anında kurur, kırılganlaşır | Duvar parçalanır |
| Sabitleyici | Mürekkep | Heykele dönüşür | Platform/siper olur |
| Silgi | Islak Yağlı Boya | Pigment çevreye bulaşır | Yeni geçici yol oluşur |
| Silgi | Mürekkep organı | İlgili davranış kaybolur | Yaratık zararsızlaştırılır |
| Tiner | Yağlı Boya | Çözünür ve akışkanlaşır | Altındaki rota da kaybolur |
| Tiner | Mürekkep | Leke havuzuna dönüşür | Leke oyuncu akış kullanır |
| Sünger | Suluboya | Emilir ve taşınabilir | Başka tuzağa boşaltılır |
| Rulo | Boş Tuval | İnce geçici zemin | Ressam kolayca bozabilir |
| Vernik | Aktif alan | Kısa süre durum kilitlenir | Kötü konum da kilitlenebilir |
| Mürekkep | Yağlı Boya | Zırhlı/katı çizim | Ağır ve yavaş yaratık |

## 17.2 Tasarım kuralı

Yeni bir boya, araç veya yaratık eklenmeden önce en az üç mevcut sistemle anlamlı etkileşimi tanımlanmalıdır. Tek amaçlı içerik oyuna eklenmemelidir.

---

# 18. TUVAL BÜTÜNLÜĞÜ

## 18.1 Amaç

Tuval Bütünlüğü, Ressamların sınırsız silme/yıkım yapmasını ve Figürlerin tinerle dümdüz kestirme açmasını engelleyen ortak sistemdir.

## 18.2 Bütünlüğü düşürenler

- Geniş silme
- Büyük yırtıklar
- Aşırı tiner
- Aynı bölgede yoğun ağır boya
- Çevresel kopma
- Kontrolsüz Mürekkep kütlesi
- Figürlerin yanlış kesimi

## 18.3 Düşük bütünlük etkileri

- Tuval lifleri görünür hâle gelir.
- Yerçekimi/kamera hafif dalgalanır.
- Boyalar beklenmedik yönde akar.
- Çizimler kararsızlaşır.
- Bazı yüzeyler yırtılmaya başlar.
- Çerçeve çıkışı dengesizleşir.

## 18.4 Sıfır bütünlük

**[KİLİTLİ]** Ressam takım tuvali kasıtlı yok ederek kazanamaz.

Ressam kaynaklı çöküşte:

- Ressam takım ağır ceza alır.
- Figürlere mesafe/çıkış telafisi verilir.
- Tur rekabetçi modda geçersiz veya Figür lehine sonuçlanabilir.

Figür kaynaklı aşırı yıkımda:

- Figür skoru düşer.
- Kestirme açmanın bedeli olur.

Kesin formül oyuncu testleriyle belirlenir.

---

# 19. TELEGRAPH VE KARŞI OYUN DİLİ

Her Ressam rolünün hem görsel hem sesli ön işareti bulunur.

| Rol | Görsel telegraph | Ses telegraph |
|---|---|---|
| Yağlı Boya | Geniş fırça gölgesi, renkli ön iz | Kılların sürtünmesi, boya kabı sesi |
| Mürekkep | Sembol eskizi, siyah damla | Kalem çizik sesi, fısıltılı mürekkep |
| Suluboya | Nem halkası, renk sızıntısı | Su ve kâğıt emiş sesi |
| Silgi | Liflerin beyazlaması, titreşim | Kuru sürtünme ve kâğıt yırtılma sesi |

Telegraph süresi saldırının gücü, alanı ve kaçınılmazlığıyla orantılıdır.

**[YASAK]** Görünmeyen alandan sessiz tek vuruş, imleçle anında hedef silme, Figürün kontrolünü uzun süre tamamen kapatma.

---

# 20. HARİTA TASARIMI — THE UNFINISHED PILGRIMAGE

## 20.1 Harita vaadi

İlk ana harita, oyunun dört malzemesini sırayla öğretir ve finalde birleştirir. Figürler tablonun sol altındaki eksik eskizden sağ üstteki yırtık çerçeveye ulaşır.

## 20.2 Makro rota

1. Eskiz Ovası
2. Impasto Köyü
3. Perspektif Ormanı
4. Suluboya Denizi
5. Mürekkep Savaşı
6. Beyaz Boşluk
7. Çerçeve Kaçışı

## 20.3 Bölge 1 — Eskiz Ovası

Amaç: Temel hareket ve Ressam telegraph dilini öğretmek.

Özellikler:

- Kurşun kalem yollar
- İnce ama güvenli başlangıç rotaları
- Basit Yağlı Boya duvarları
- Küçük silinebilir köprüler
- İlk halat kurtarması

Ressam baskısı düşük, okunabilirlik yüksek.

## 20.4 Bölge 2 — Impasto Köyü

Amaç: Islak/kuru boya ve fiziksel şekillendirme.

- Kalın boya evleri
- Kesilebilir duvarlar
- Yuvarlanabilir pigment varilleri
- Dar sokak kombinasyonları
- Çatılardan kestirme

## 20.5 Bölge 3 — Perspektif Ormanı

Amaç: Resim perspektifini oynanışa dönüştürmek.

- Uzak görünen nesneler gerçek anlamda küçük olabilir.
- Farklı resim katmanları arasında atlama yapılır.
- Silgi belirlenmiş nesnelerin ölçeğini değiştirir.
- Figür Büyüteci burada değer kazanır.

**[HİPOTEZ]** Perspektif değişimi okunabilirlik testinden geçmezse yalnızca sabit geçitlerde kullanılacaktır.

## 20.6 Bölge 4 — Suluboya Denizi

Amaç: Akış, ekipman taşıma ve yüksek zemin.

- Kâğıt tekneler
- Eğimle değişen deniz
- Süngerle su taşıma
- Mürekkebin suda büyümesi
- Yağlı Boya ile set/baraj yapma

## 20.7 Bölge 5 — Mürekkep Savaşı

Amaç: Üçüncü taraf kontrolsüz çizimler.

- Eski tablodan kalan tarafsız Mürekkep canlıları
- İki takıma da tepki verirler
- Ressam onları kendi sembolleriyle yönlendirebilir
- Kontrol kaybı iki tarafı da cezalandırır

## 20.8 Bölge 6 — Beyaz Boşluk

Amaç: Figürlere sınırlı yaratım gücü vermek.

- Hazır zemin çok azdır.
- Figürler topladıkları pigmentle ince yollar oluşturur.
- Ressamlar bu yolları bozabilir veya kendi boya sistemine katabilir.
- Takım kaynak kararı final skorunu etkiler.

## 20.9 Bölge 7 — Çerçeve Kaçışı

Amaç: Bütün sistemlerin final kombinasyonu.

- Tuval gerçek dünyaya doğru yırtılır.
- Oyuncular fırça, kalem, boya tüpü ve çerçeve süslerine tırmanır.
- Ressamların çalışma masası artık oynanabilir çevrenin parçasıdır.
- Çıkış tek oyuncuyla bitmez; her çıkan Figür puan getirir.

---

# 21. ROTA TASARIM KURALLARI

Her büyük encounter alanında:

- Bir ana okunabilir rota
- Bir riskli kestirme
- Bir Ressam kombinasyonuna uygun darboğaz
- Bir Figür takım aracıyla açılan alternatif
- En az bir Ressam hatasının Figür avantajına dönüşebileceği yüzey
- Kurtarma için tutunma noktaları

bulunmalıdır.

**[YASAK]** Tek koridorda kaçınılmaz duvar spam’i, belirsiz dekor arasında kaybolan rota, yalnızca bir özel eşya varsa geçilebilen zorunlu kapı.

---

# 22. SKORLAMA

## 22.1 Tasarım hedefi

Skor, yalnızca en hızlı oyuncuyu değil takım bütünlüğünü ve akıllı kaynak kullanımını ödüllendirir.

## 22.2 Önerilen ilk formül

Her Figür:

- Mesafe: 0–1.000
- Çerçeveden çıkış: +250
- Temiz pigment özü: +50
- Kritik takım kurtarması: sınırlı +25
- Yüksek Tuval Bütünlüğü takım bonusu: 0–150 toplam
- Figür kaynaklı gereksiz büyük yırtılma: eksi

Değişken Figür sayısının doğrudan skor üstünlüğü yaratmaması için sonuç iki biçimde gösterilir:

- Ham takım puanı: Bütün Figür puanları ve takım bonuslarının toplamı
- Normalize performans: Oyuncu başına ortalama mesafe, çıkış oranı ve takım bonusunun kişi sayısına bölünmüş değeri

Frame Versus eşit takım sayısı kullandığı için ham puan karşılaştırılabilir. Expedition ve Rotation sonuç ekranlarında normalize performans ana karşılaştırma değeridir.

## 22.3 Eşitlik bozma

Sıra:

1. Çıkan Figür sayısı
2. Toplam mesafe
3. Tuval Bütünlüğü
4. Tamamlama süresi
5. Harcanmamış kritik kaynak

## 22.4 Skor suistimali önleme

- Aynı oyuncuyu tekrar tekrar kaldırarak puan kasılamaz.
- Ressam kendi tuvalini yıkarak rakip mesafesini sıfırlayamaz.
- Figür güvenli alanda zaman geçirerek sonsuz kaynak yenileyemez.
- Leke durumunda çıkış tam Figür puanı vermez.

---

# 23. DENGE VE GERİ DÖNÜŞ

## 23.1 Anti-snowball ilkesi

Gerideki tarafa ücretsiz güç verilmez; daha riskli ama etkili fırsatlar verilir.

Örnekler:

- Hasarlı tuvalden kısa rota
- Kontrolsüz Mürekkep sürüsünü salma
- Ressam pigment kuyusundan çalma
- Ağır ama güçlü temizleme aracı
- Skor karşılığı hızlı restore

## 23.2 Ressam baskı bütçesi

Ressamlar bütün güçlü yeteneklerini aynı saniyede sınırsız kullanamaz.

**[HİPOTEZ]** Takım çapında “Kompozisyon Gerilimi” sistemi:

- Büyük yetenekler ortak gerilim üretir.
- Gerilim yükselince telegraph süresi uzar veya Tuval Bütünlüğü maliyeti artar.
- Takımlar saldırıları sıraya koymak zorunda kalır.

Bu sistem bireysel cooldown’ların yeterli olmaması hâlinde prototiplenecektir.

Atölye Bütçesi Kompozisyon Gerilimi’nin yerine geçmez:

- Atölye Bütçesi oyuncu oranına göre maç başlangıç kapasitesini belirler.
- Kompozisyon Gerilimi maç sırasında güçlü eylemlerin üst üste yığılmasını sınırlar.
- İki sistem birlikte tek Ressamın sıkıcı derecede yavaş, dört Ressamın ise okunamaz derecede yoğun olmasını önler.

## 23.3 Oyuncu sayısı eksikliği

- Kısa süreli bağlantı kopmasında bot devralır.
- Ressam botu karmaşık çizim yerine destek rolüne geçer.
- Figür botu takım formasyonunu ve kurtarmayı önceler.
- Rekabetçi maçta uzun süreli eksiklik için yeniden bağlanma penceresi bulunur.

---

# 24. OYUN MODLARI

## 24.1 Atelier Escape — Önerilen arkadaş modu

Varsayılan preset 2 Ressam – 4 Figürdür.

- Tek kesintisiz yolculuk
- 10–18 dakika hedef süre
- Rol değişimi zorunlu değil
- Altı kişilik arkadaş gruplarına uygun
- İstenirse üç turlu Rotation’a dönüştürülebilir
- Casual matchmaking ancak yeterli oyuncu havuzu doğrulanırsa açılır

## 24.2 Frame Versus — Rekabetçi ana mod

4v4, iki yarı, zorunlu rol değişimi, aynı seed ve resmî skor.

## 24.3 Guided Escape — Eğitim/co-op

1–6 Figür, yapay zekâ Ressam/Curator.

Amaç:

- Figür araçlarını öğretmek
- Haritayı tanıtmak
- Rekabet baskısı olmadan boya etkileşimi göstermek

## 24.4 Studio Lobby — Özel maç

Ayarlanabilir:

- Pigment miktarı
- Tur süresi
- Serbest çizim sınırı
- Friendly interaction
- Günlük mutasyonlar
- 1–6 Figür ve 1–4 Ressam koltuğu
- Expedition, Rotation veya Frame Versus
- Palet Yükü ve rol tekrarları
- Skor/rekabetçi eşitleme

## 24.5 Gallery Chaos

2 Ressam – 6 Figür veya lobi tarafından seçilen geniş kompozisyon; skor yerine parti odaklı kurallar. Ranked dengesini belirlemez.

## 24.6 [SONRA] Workshop Canvases

Kullanıcıların resim katmanları ve rota düğümleri oluşturması. Serbest kod veya kontrolsüz içerik üretimi değil, güvenli modüler araç seti.

---

# 25. META İLERLEME

## 25.1 Güç eşitliği

**[KİLİTLİ]** Kalıcı ilerleme rekabetçi güç vermez. Yeni oyuncu, açılmış istatistik avantajı nedeniyle kaybetmez.

## 25.2 Kozmetik açılımlar

Figür:

- Restoratör kıyafetleri
- Kontur stilleri
- Leke animasyonları
- Çanta ve araç görünümleri
- Zafer/yenilgi pozları

Ressam:

- Eldiven
- Fırça sapı
- Boya kabı
- Kıl izi
- İmleç şekli
- Masa dekoru

## 25.3 Rozetler

- Rakibin duvarını kestirme olarak kullan
- Leke hâlinden restore olup çık
- Tek kombinasyonla Figür takımını en az üç gruba ayır
- Kendi Mürekkep yaratığını takımına zarar vermeden geri çek
- %90+ Tuval Bütünlüğüyle tamamla
- Dev fırça üzerinde belirli mesafe seyahat et

## 25.4 Günlük tablo

**[HEDEF]** Her gün aynı seed tüm oyunculara sunulabilir.

Mutasyon örnekleri:

- Mürekkep suya değince ikiye bölünür.
- Yağlı boya geç kurur.
- Silinen temel katman geri büyür.
- Suluboya yukarı akar.
- Figürler düşük Netlikle başlar fakat daha çok araç bulur.

---

# 26. SOSYAL VE VİRAL TASARIM

## 26.1 Doğal klip ilkesi

Viral anlar önceden yazılmış komik animasyonlar değil sistem birleşimleri olmalıdır.

Örnekler:

- Ressam duvar çizer, Figür onu rampa yapıp saldırının üstünden atlar.
- Sünger patlar ve bütün takım farklı renklere bulanır.
- Mürekkep yaratığı yanlış akıntıyla Ressam planını bozar.
- Leke oyuncu, düşman yaratığın içine girip onu uçuruma sürükler.
- Silgi geri dönüşü kapatmaya çalışırken kestirme açar.
- Figür dev fırça kılına tutunup haritanın ilerisine taşınır.

## 26.2 Sesli iletişim

- Takım içi ses sürekli kullanılabilir.
- Rakip takım normalde net duyulmaz.
- Belirli tuval yırtıkları/ince bölgelerde rakip sesleri kısa süre sızabilir.
- Ressam fırça vuruşları Figürlere fiziksel titreşim olarak gider.

## 26.3 Tekrar ve klip sistemi

**[SONRA/HEDEF]** Sunucu olay komutlarını kaydettiği için hafif tekrar sistemi oluşturulabilir:

- Son 20 saniye
- Fırça ve boya olayları
- Çoklu kamera
- Otomatik “Masterstroke”, “Last Figure”, “Friendly Mistake” işaretleri

---

# 27. UI/UX

## 27.1 Figür HUD

Minimal ve diegetik hedef:

- Dört takım portresi ve Netlik durumu
- Ana araç dayanıklılığı
- İki sarf malzemesi
- Tuval Bütünlüğü
- Çıkış yönü/mesafe ilerlemesi
- Yaklaşan büyük Ressam eylemi için yönsel uyarı

Sağlık sayıları yerine karakter portresinin kontur bozulması kullanılmalıdır.

## 27.2 Ressam HUD

- Seçili rol ve fırça tipi
- Pigment haznesi
- Fırça kalınlığı/basıncı
- Islak-kuru göstergesi
- Takım arkadaşlarının rol durumları
- Figür ilerleme hattı
- Tuval Bütünlüğü
- Çizim yasak alanı geri bildirimi
- Mürekkep karmaşıklık bütçesi

## 27.3 Renk körlüğü

Malzemeler yalnızca renk ile ayrılmaz:

- Doku
- Parlaklık
- Kenar şekli
- Partikül davranışı
- Ses
- İkon

ile de tanımlanır.

---

# 28. SES TASARIMI

## 28.1 Sesin amacı

Oyuncu bakmadan yaklaşan malzemeyi anlayabilmelidir.

## 28.2 Malzeme sesleri

- Yağlı Boya: ağır, yapışkan, kalın fırça sürtünmesi
- Mürekkep: kuru kalem, kâğıt çizilmesi, hafif fısıltı
- Suluboya: emilen su, kâğıt lifleri, akış
- Silgi: kuru sürtünme, lif kopması, vakum hissi
- Tiner: ince çözünme ve yüzey dalgalanması
- Vernik: cam benzeri kilitlenme

## 28.3 Ölçek sesi

Figür açısından dev fırça yaklaşımı:

- Masa titreşimi
- Gölgede düşük frekans
- Kılların rüzgârı
- Boyanın zemine çarpması

Ressam açısından aynı olay daha yakın ve dokunsal duyulur.

## 28.4 Müzik

Dinamik katmanlar:

- Harita sanat stiline ait temel tema
- Ressam hazırlığı sırasında ritmik gerilim
- Kombinasyon saldırısında perküsyon
- Son Figür ve finalde yükselen fakat iletişimi boğmayan müzik

---

# 29. ERİŞİLEBİLİRLİK

- Tam tuş yeniden atama
- Gamepad çizim yardımı
- Fırça otomatik yumuşatma ayarı
- Renk körlüğü profilleri
- Kamera sarsıntısı azaltma
- Hareket bulanıklığı kapatma
- Ufuk stabilizasyonu
- Büyük UI ve altyazı seçenekleri
- Ses telegraph’ları için görsel ikonlar
- Görsel telegraph’lar için yönsel ses
- Basılı tutma/toggle seçenekleri
- Hızlı iletişim tekeri
- Eğitimde yavaşlatılmış fırça telegraph’ı

---

# 30. SANAT YÖNETİMİ

## 30.1 Ana stil

**[KİLİTLİ]** Fotogerçekçi insanlardan ziyade dokunsal, el yapımı, sofistike ve hafif tekinsiz sanat dünyası.

## 30.2 Kaçınılacak çizgiler

- Çocuk boyama uygulaması
- Aşırı gökkuşağı ve plastik oyuncak estetiği
- Splatoon benzeri arena/renk savaşı görünümü
- Epic Mickey/Dreams gibi mevcut IP’lerin belirgin görsel kopyası
- Horizon benzeri kabile teknoloji dili
- Normal 3D dünya üzerine yalnızca boya kaplaması

## 30.3 Materyal hedefleri

Yağlı boya:

- Bristle groove
- Kalınlık
- Islak specular
- Kuruma çatlağı
- Kenarlarda pigment birikimi

Mürekkep:

- Liflere kanama
- Keskin çekirdek
- Akışkan uzuv geçişi
- Siyah içinde sınırlı highlight

Suluboya:

- Şeffaf üst üste binme
- Pigment kenar birikmesi

- Kâğıt emişi
- Renk ayrışması

Tuval:

- Yakında lif detayı
- Uzakta temiz okunabilir ton
- Yırtıldığında fiziksel iplikler

## 30.4 Figür silueti

- Uzakta okunur büyük el/ayak
- Basit gövde oranı
- Takım ve araç rengi ayrımı
- Kontur Netlik durumunu taşır
- Çok küçük detay yerine güçlü animasyon

---

# 31. GELECEK HARİTA KONSEPTLERİ

## 31.1 The Anatomy Lesson

- Eski anatomi çizimi
- Kas çizgileri rota
- Kemikler platform
- Organ eskizleri canlı sistem
- Silgi anatomik işlevi değiştirir

## 31.2 The Last Ukiyo-e

- Baskı kalıpları
- Sınırlı renk katmanları
- Büyük dalga hareketi
- Mürekkep ve su merkezli fizik

## 31.3 Child’s Last Drawing

- Pastel ve yanlış perspektif
- Canlı güneş/ev sembolleri
- Daha komik fizik
- Ton yine çocuk oyunu değil, nostaljik ve hafif tekinsiz

## 31.4 The Cubist City

- Aynı nesnenin farklı açıları eşzamanlı
- Düzlem geçişleri
- Parçalı vücut ve rota
- Perspektif rolü güçlenir

## 31.5 Black Engraving

- Siyah/beyaz çizgi dünyası
- Tarama çizgileri zemin
- Silgi ışık yaratır
- Yüksek kontrast ve ses takibi

**[SONRA]** Yeni harita üretimi, temel haritanın retention ve denge verileri doğrulandıktan sonra başlar.

---

# 32. UNITY TEKNİK MİMARİSİ

## 32.1 Motor ve render hattı

**[HEDEF]** Unity 6 LTS.

Önerilen başlangıç:

- URP
- Shader Graph
- VFX Graph
- Input System
- Addressables
- Cinemachine
- Unity Multiplayer araçları veya doğrulanmış alternatif ağ katmanı

URP gerekçesi:

- Stilize hedef
- Orta seviye PC desteği
- Daha geniş performans marjı
- Özel boya shader’larının kaliteyi taşıması

HDRP ancak prototip sonrası görsel hedef URP ile karşılanamazsa değerlendirilir.

## 32.2 Ana kod modülleri

```text
Core
├── MatchFlow
├── LobbyComposition
├── AtelierBudget
├── TeamsAndRoles
├── Scoring
├── SharedClock
└── EventLog

Canvas
├── CanvasSurfaceGraph
├── CanvasIntegrity
├── PaintLayerRegistry
├── StrokeRuntime
├── EraseRestoreSystem
└── PerspectiveNodes

Paint
├── OilStrokeSystem
├── InkGlyphSystem
├── WaterFlowSystem
├── DryingStateSystem
└── MaterialInteractionResolver

Figures
├── FigureMotor
├── ClarityState
├── ToolSystem
├── RescueSystem
└── StainForm

Painters
├── PainterCamera
├── BrushInput
├── PigmentEconomy
├── StrokePreview
└── RoleAbilities

Network
├── ServerAuthority
├── StrokeCommandReplication
├── FigurePrediction
├── SnapshotInterpolation
└── ReconnectAndBots
```

## 32.3 Yağlı boya uygulaması

**[KİLİTLİ TEKNİK İLKE]** Sınırsız voxel dünyası veya her kare yeniden örülen dev mesh kullanılmaz.

Öneri:

1. Fırça hareketinden azaltılmış kontrol noktaları çıkarılır.
2. Sunucu geçerli yüzey ve bütçe kontrolü yapar.
3. Spline boyunca render mesh üretilir.
4. Islak aşamada basit dinamik collision kullanılır.
5. Kuruma sonunda optimize collision proxy’ye geçilir.
6. Görsel kalınlık shader ve mesh profilinden gelir.

## 32.4 Suluboya uygulaması

Tam 3D fluid simülasyonu yapılmaz.

- Tuval yüzeyinde 2D flow field
- Düşük çözünürlüklü simülasyon grid’i
- Render Texture maske
- GPU tabanlı görsel yayılım
- Oynanış için basitleştirilmiş kuvvet bölgeleri
- Belirli aralıklarla ağ senkronizasyonu

## 32.5 Mürekkep uygulaması

- Glyph tanıma serbest AI çizim analizi değildir.
- Oyuncu ikon/sembol seçer veya kolay gesture çizer.
- Semboller doğrulanmış modüler prefab parçalarına çevrilir.
- Davranış Graph/StateTree benzeri veri tanımıyla kurulur.
- Render tarafında çizilmiş görünüm korunur.
- Karmaşıklık ve eşzamanlı yaratık sayısı sınırlıdır.

## 32.6 Silgi uygulaması

- Her mesh’te keyfî boolean kesim yoktur.
- Harita silinebilir hücre/katman bölgelerine ayrılır.
- Görsel silme maske ve dissolve shader ile yapılır.
- Collision kontrollü hücre durumuyla açılır/kapanır.
- Hassas silme için alt grid bulunur.

## 32.7 Tuval yüzey grafiği

Harita yüzeyleri yalnızca mesh değil, veri grafiğidir:

- Komşuluk
- Eğim
- Akış yönü
- Boyanabilirlik
- Silinebilirlik
- Bütünlük ağırlığı
- Perspektif katmanı
- Collision modu
- Ağ bölgesi

Bu veri hem Ressam ön izlemesini hem Suluboya akışını hem AI navigasyonunu besler.

---

# 33. MULTIPLAYER VE AĞ TASARIMI

## 33.1 Sunucu otoritesi

**[KİLİTLİ]** Rekabetçi maçta:

- Figür durumu
- Boya komut geçerliliği
- Pigment harcaması
- Tuval Bütünlüğü
- Skor
- Mürekkep yaratık sahipliği

sunucu tarafından doğrulanır.

Sunucu ayrıca maç başlamadan önce lobi kompozisyonunu doğrular:

- En az 1 Figür ve 1 Ressam
- En fazla 6 Figür ve 4 Ressam
- Seçilen modun izin verdiği rol değişimi yapısı
- Disiplin tekrarları ve Palet Yükü
- Preset’e ait Atölye Bütçesi profili
- Ranked için tam 4v4 kadro ve iki yarı zorunluluğu

## 33.2 Çizim replikasyonu

Ağ üzerinden tam doku/video gönderilmez. Stroke command gönderilir:

```text
StrokeId
PainterId
MaterialType
SurfaceId
ControlPoints[]
StartTick
Width
PressureProfile
PigmentCost
Seed
```

İstemciler aynı görseli deterministik veya yakın eşdeğer biçimde yerelde üretir.

## 33.3 Figür hareketi

- Client-side prediction
- Server reconciliation
- Snapshot interpolation
- Hareketli boya yüzeyleri için referans yüzey ID’si
- Halat ve grab durumları için özel senkronizasyon

## 33.4 Mürekkep AI

- Otoriter karar sunucuda
- Görsel ara pozlar istemcide
- Uzak küçük yaratıklar düşük güncelleme hızında
- Sahiplenilen yaratık Figür karakteri benzeri prediction kullanır

## 33.5 Yeniden bağlanma

- Oyuncu slotu belirli süre korunur.
- Bot geçici devralır.
- Oyuncu aynı rol ve kaynak durumuna döner.
- Stroke history’den dünya yeniden oluşturulabilir.

## 33.6 Maç tekrar üretimi

Event log:

- Seed
- Stroke command’ları
- Silme/restore olayları
- Oyuncu durum geçişleri
- Skor olayları

saklanarak hata tekrar üretme ve ileride replay desteği sağlanır.

---

# 34. PERFORMANS BÜTÇELERİ

## 34.1 Hedef

**[HEDEF]** 1080p/60 FPS orta seviye PC; 1440p upscaling destekli yüksek ayarlar.

Kesin minimum donanım vertical slice profiling sonrası belirlenir.

## 34.2 CPU bütçesi ilkeleri

- Her boya lekesi ayrı Update alan GameObject olmaz.
- Stroke’lar bölgesel batch edilir.
- Kurumuş stroke’lar statik/uyuyan duruma geçer.
- Uzak Mürekkep yaratıkları basit simülasyona düşer.
- Suluboya grid çözünürlüğü oyun alanına göre adaptiftir.
- Collision yalnızca aktif oyuncu çevresinde ayrıntılıdır.

## 34.3 GPU bütçesi ilkeleri

- Tuval materyal varyantları kontrollü
- Texture array/atlas
- VFX overdraw sınırı
- Uzak stroke mesh basitleştirme
- Dinamik ışık sayısı sınırlı
- Boya kalınlığı için pahalı tessellation yerine kontrollü mesh profil

## 34.4 Ağ bütçesi ilkeleri

- Stroke noktaları quantize ve sadeleştirilir.
- Her frame fırça pozisyonu gönderilmez.
- Görsel partiküller replike edilmez.
- Suluboya tam grid’i değil olaylar ve periyodik düzeltme gönderilir.
- Normal üretim hedefi 2 Ressam – 4 Figürdür; maksimum 4 Ressam – 6 Figür lobi stresi ayrıca profillenir.
- Ressam sayısı arttığında oyuncu başına stroke gönderim bütçesi dinamik olarak düşer.
- Lobi kompozisyonu maç boyunca değişmez; bağlantı kopmalarını geçici botlar devralır.

---

# 35. VERİ ODAKLI TASARIM

Yetenekler kod içine sabitlenmek yerine veri tanımı kullanır.

Örnek PainterAbility tanımı:

```yaml
id: oil_impasto_wall
role: oil
pigment_cost: 35
telegraph_seconds: 1.4
active_seconds: 8
integrity_weight: 4
counter_tags:
  - palette_knife
  - solvent
  - early_fixative
interaction_tags:
  - water_pushable_when_wet
  - ink_armorable
```

Bu yapı:

- Denge ayarını hızlandırır.
- Botların yetenek okumasını kolaylaştırır.
- Günlük mutasyonları mümkün kılar.
- Test otomasyonu sağlar.

---

# 36. BOT VE CURATOR SİSTEMİ

## 36.1 Curator

Curator, hile yapan gizli yönetmen değil maç temposunu düzenleyen deterministik encounter sistemidir.

Görevleri:

- Tarafsız Mürekkep canlılarını yönetmek
- Çevresel olay zamanını uygulamak
- Eşit seed’de aynı olayları üretmek
- Takım durumuna göre yalnızca önceden tanımlı fırsat havuzunu seçmek

Rekabetçi iki yarıda adalet için Curator kararları aynı girdilerle tekrar edilebilir olmalıdır.

## 36.2 Figür botu

Öncelikler:

1. Takımla kal
2. Açık telegraph’dan kaç
3. Düşen oyuncuyu kurtar
4. Rol aracını basit karşılıkta kullan
5. Ana rotaya ilerle

## 36.3 Ressam botu

Karmaşık özgür çizim yerine:

- Hazır stroke şablonları
- Darboğaz işaretleri
- Takım arkadaşının combo ping’ine cevap
- Restorasyon ve destek

Bot, insan Ressamın yaratıcılığını taklit etmek zorunda değildir.

---

# 37. MODERASYON VE UYGUNSUZ ÇİZİM

## 37.1 Risk

Serbest fırça, uygunsuz şekil ve taciz içeriği oluşturabilir.

## 37.2 Genel matchmaking çözümü

- Stroke otomatik yumuşatma
- Minimum/maksimum çizgi boyu
- Yağlı boya fiziksel profile dönüşür; düz 2D çizim olarak gösterilmez
- Mürekkep yaratıkları yalnızca glyph gramerinden oluşur
- Kalıcı serbest yazı yok
- İsim/mesaj çizimine izin verilmez
- Rapor ve replay event incelemesi

## 37.3 Özel lobi

Daha özgür çizim ayarı sunulabilir; fakat yayın/ekran görüntüsü güvenlik kontrolleri ayrıca değerlendirilir.

---

# 38. GÜVENLİK VE HİLE ÖNLEME

- Sunucu pigment maliyetini doğrular.
- Stroke noktaları hız/mesafe/alan sınırından geçirilir.
- Yasak yüzey çizimleri reddedilir.
- Figür Netlik ve araç kullanımı sunucu otoriterdir.
- İstemci skor hesaplamaz.
- Şüpheli çizim hızı ve input otomasyonu izlenir.
- Replay/event log rapor incelemesine bağlanır.

---

# 39. MVP TANIMI

## 39.1 MVP’nin amacı

MVP “tam oyunun küçük hâli” değil, ana eğlence tezini kanıtlar:

> Bir insanın başka insanların önüne gerçek zamanlı fiziksel boya çizmesi, iki taraf için de okunabilir, karşılıklı ve tekrar oynanabilir bir rekabet yaratıyor mu?

## 39.2 MVP içeriği

- 1–4 Figür ve 1–2 Ressam arasında değişken lobi kurulumu
- Ana ağ testi olarak 2 Ressam – 4 Figür
- Tek kısa harita: Eskiz Ovası + Impasto Köyü
- Yağlı Boya rolü tam
- Basit Mürekkep rolü
- Suluboya temel yıkama
- Silgi hassas silme
- Dört Figür ana aracı
- Netlik ve Leke sistemi
- Mesafe/çıkış skoru
- Tek Expedition koşusu
- Basit Rotation testi
- Oyuncu oranına göre pigment/aktif stroke ölçeklemesi
- Temel sesli iletişim
- Gri kutu veya sınırlı sanat kalitesi

## 39.3 MVP dışı

- Beş tam harita
- Kozmetik mağaza
- Workshop
- Ranked sezon
- Gelişmiş replay
- Konsol
- Serbest AI yaratık üretimi
- Tam hikâye kampanyası
- Fotogerçekçi materyal hedefi

---

# 40. PROTOTİP AŞAMALARI

## Aşama 0 — Tasarım doğrulama

- Fırça spline prototipi
- Islak/kuru durum
- Figürün duvarı kesmesi
- Tek cihazda iki kontrol veya bot hedef

Başarı kriteri:

- Fırça kullanımı ilk 2 dakikada anlaşılır.
- Figür duvarı yalnız engel değil fırsat olarak da kullanabilir.
- Çizim gecikmesi rahatsız etmeden telegraph sağlar.

## Aşama 1 — 1v1 eğlence prototipi

- Bir Figür
- Bir Yağlı Boya Ressamı
- Beş dakikalık koridor
- Üç rota düğümü

Ölçülecek:

- Ressamın etkili hamle sıklığı
- Figürün çaresiz hissettiği anlar
- Duvar başına karşı oyun çeşidi
- Tekrar oynama isteği

## Aşama 2 — Malzeme laboratuvarı

- Dört malzeme
- İki Figür
- Kombinasyon odaları

Ölçülecek:

- Malzemeler uzaktan ayırt ediliyor mu?
- Kombinasyon sonucu önceden tahmin edilebilir mi?
- Hata komik mi, rastgele mi?

## Aşama 3 — Esnek lobi ağ prototipi

- Placeholder sanat
- 1v1, 2 Ressam–4 Figür ve 4v4 test preset’leri
- Expedition ve iki kısa Frame Versus yarısı
- Pigment ekonomisi
- Atölye Bütçesi
- Skor
- Reconnect

Başarı kriteri:

- Altı oyunculuk ana sosyal formatta stroke senkronizasyonu stabil.
- Sekiz oyunculuk 4v4 ve on oyunculuk maksimum lobi stres testini tamamlayabiliyor.
- Figür takımı en az üç farklı karşı plan üretiyor.
- İki Ressam en az iki koordineli combo kurabiliyor.
- Tek, iki ve dört Ressamlı testlerde baskı yoğunluğu kabul edilebilir aralıkta kalıyor.
- Kaybeden takım neden kaybettiğini açıklayabiliyor.

## Aşama 4 — Vertical slice

- Impasto Köyü yüksek kalite
- Dört rolün temsilî tam seti
- 12 dakikalık yarı
- Final mini çerçevesi
- Profesyonel ses/VFX/UI

Amaç:

- Yatırımcı/yayıncı sunumu
- Steam duyuru materyali
- Kapalı topluluk testi
- Performans ve üretim maliyeti tahmini

---

# 41. ÜRETİM YOL HARİTASI

## Faz 1 — Pre-production

- Tasarım sütunlarını doğrulama
- Teknik spike’lar
- Görsel dil testi
- Ağ mimarisi kararı
- Riskli sistem prototipleri

## Faz 2 — Vertical slice

- Tek cilalı bölge
- Cilalı 2 Ressam–4 Figür Atelier Escape döngüsü
- İşlevsel 4v4 Frame Versus döngüsü
- Değişken lobi ve Atölye Bütçesi doğrulaması
- Kullanıcı testleri
- Pazarlama tezinin doğrulanması

## Faz 3 — Alpha

- Ana haritanın tüm bölgeleri
- Tüm araçlar
- Temel progression
- Matchmaking
- Botlar

## Faz 4 — Beta

- Denge
- Performans
- Erişilebilirlik
- Anti-cheat/moderasyon
- Sunucu yükü
- Büyük oyuncu testi

## Faz 5 — Launch hazırlığı

- Tutorial/co-op
- Steam sayfası ve demo
- Creator testleri
- Crash/telemetry
- Lokalizasyon
- İlk gün yaması

---

# 42. EKİP İHTİYACI

Minimum güçlü çekirdek ekip:

- 1 gameplay/network programcısı
- 1 graphics/technical artist-programmer
- 1 game designer/producer
- 1 environment/material artist
- 1 character/animation generalist
- Part-time ses/VFX/UI desteği

Daha küçük ekipte roller birleşebilir; ancak aşağıdaki yetkinlikler mutlaka karşılanmalıdır:

- Ağlı karakter hareketi
- Runtime mesh/spline
- Shader ve render texture
- Asimetrik oyun dengesi
- Teknik sanat optimizasyonu
- Kullanıcı testi ve telemetry

---

# 43. TEST PLANI VE METRİKLER

## 43.1 Eğlence metrikleri

- Maçtan sonra “bir tur daha” oranı
- İkinci yarıya kalan oyuncu oranı
- Takım başına koordineli combo sayısı
- Ressam çiziminin Figür tarafından ters kullanım sayısı
- Leke oyuncunun aktif kaldığı süre
- Maç başına kurtarma sayısı

## 43.2 Denge metrikleri

- Figür ortalama mesafesi
- Rol bazında pigment verimliliği
- Araç seçilme ve başarı oranı
- Birleşik saldırı sonrası Netlik kaybı
- Figürlerin ayrılma süresi
- Tuval Bütünlüğü çöküş nedenleri
- İki yarı avantaj farkı

## 43.3 Okunabilirlik test soruları

Oyuncuya maç sonrası sorulur:

- Seni hangi malzeme etkiledi?
- Saldırıyı önceden gördün mü?
- Hangi karşı hamleyi kullanabilirdin?
- Takım arkadaşının rolünü anlayabildin mi?
- Çıkışın yönünü kaybettin mi?
- Ölüm/leke durumunun nedenini anladın mı?

## 43.4 Teknik metrikler

- CPU/GPU frame time
- Stroke başına ağ verisi
- Aktif collision sayısı
- Kurumuş mesh batch sayısı
- Suluboya simülasyon maliyeti
- Reconciliation düzeltme sıklığı
- Disconnect/reconnect başarı oranı

---

# 44. TUTORIAL

Tutorial tek seferde bütün sistemi anlatmaz.

## Ders 1 — Çizilen dünyayı oku

- Fırça gölgesi
- Islak boya
- Basit kaçış

## Ders 2 — Boyayı kullan

- Duvarı kes

- Rampaya dönüştür
- Takım arkadaşını çek

## Ders 3 — Malzemeler

- Yağlı Boya
- Mürekkep
- Suluboya
- Silgi

## Ders 4 — Leke ve restore

- Kontrollü biçimde Leke ol
- Süngere gir
- Restore edil

## Ders 5 — Ressam rolü

- Ön izleme
- Pigment
- Fırça basıncı
- Figür karşı oyununu gözleme

## Ders 6 — Mini Versus

- Kısa 2v2 bot maçı
- Rol değişimi
- Skor açıklaması

---

# 45. MATCHMAKING VE RANKED

## 45.1 İlk sürüm

- En fazla bir veya iki test edilmiş casual preset kuyruğu
- 1–4 Ressam ve 1–6 Figür seçilebilen özel Studio Lobby
- Reconnect
- Rol tercihi göstergesi
- Takım halinde sıra

Oyuncu havuzu bölünmemelidir. Her olası lobi oranı için ayrı genel kuyruk açılmaz. İlk adaylar:

- Atelier Escape: 2 Ressam – 4 Figür
- Frame Versus: 4v4

Canlı oyuncu sayısı iki kuyruğu sağlıklı tutmuyorsa yalnızca biri hızlı oyun olarak açık kalır; diğer format özel lobiden oynanır.

## 45.2 [SONRA] Ranked

Ranked, temel denge ve oyuncu havuzu doğrulanmadan çıkmaz.

Gereksinimler:

- Sabit 4v4 kadro ve tam iki yarı
- Rol kaçış cezası
- Harita/seed adaleti
- Anti-cheat
- Minimum tutorial tamamlama
- MMR’nin takım ve rol performansını aşırı bölmemesi

---

# 46. TİCARİ MODEL VE LANSMAN İLKELERİ

## 46.1 Fiyat konumu

**[HİPOTEZ]** Düşük/orta fiyatlı premium oyun. Kesin fiyat pazar analizi, kapsam ve sunucu maliyetinden sonra belirlenir.

## 46.2 Satış yaklaşımı

- Tek cümlelik mekanik
- Oynanıştan gerçek klipler
- Farklı büyüklükteki arkadaş gruplarının reaksiyonu
- “Ressam gözü” ve “Figür gözü” karşılaştırması
- Bedava oynanabilir demo veya süreli playtest

## 46.3 Kozmetik

- Rekabetçi güç satılmaz.
- Görsel okunabilirliği bozan fırça/iz satılmaz.
- Harita malzeme rengini rakipten saklayan kozmetik olmaz.
- Ücretli içerik takım bölünmesine yol açmamalıdır.

---

# 47. ANA RİSKLER VE ÇÖZÜMLER

## 47.1 Ressam çok güçlü

Belirti:

- Figür kendini çaresiz hisseder.
- Aynı darboğazda sürekli ölür.

Çözüm:

- Uzun telegraph
- Çizim yasak hacmi
- Pigment maliyeti
- Çoklu karşı araç
- Alternatif rota
- Ressam hatasının avantaj üretmesi

## 47.2 Ressam sıkıcı

Belirti:

- Cooldown bekler.
- Figürleri uzaktan izler.

Çözüm:

- Fırça fizik kontrolü
- Küçük düşük maliyetli eylemler
- Mürekkep sahiplenme
- Kurumuş boya yeniden şekillendirme
- Takım arkadaşının çizimine destek

## 47.3 Figür tarafı yalnız kaçış

Çözüm:

- Araçlar
- Boyayı ters kullanma
- Leke desteği
- Rota yaratma
- Takım kurtarma
- Pigment çalma

## 47.4 Teknik serbestlik kontrol edilemiyor

Çözüm:

- Spline stroke
- Hücre tabanlı silme
- Flow field suluboya
- Glyph yaratıkları
- Veri odaklı yüzey grafiği

## 47.5 Görsel kaos okunmuyor

Çözüm:

- Katman yoğunluğu sınırı
- Güçlü materyal silueti
- UI yön uyarısı
- Partikül bütçesi
- Figür konturu
- Aynı alanda maksimum aktif büyük stroke

## 47.6 İçerik az geliyor

Çözüm:

- Sistem kombinasyonları
- Günlük mutasyon
- Rol ustalığı
- Seed
- İlk haritanın yüksek tekrar oynanabilirliği
- Yeni harita yerine önce yeni etkileşim

---

# 48. YAPILMAYACAKLAR LİSTESİ

**[YASAK]**

- Ressamın Figürü imleçle doğrudan silmesi
- Ana oynanışın fırçayla insan kovalamaya dönüşmesi
- Tam serbest voxel/akışkan simülasyonu
- Gerçek dünya kadar karmaşık paint chemistry
- Her oyuncunun tüm Ressam yeteneklerine aynı anda sahip olması
- Figürlerin yalnızca koşup kaçması
- Uzun süreli seyirci modu
- Pay-to-win progression
- İlk sürümde beş farklı harita yapmaya çalışma
- Fotogerçekçi AAA görsel zorunluluğu
- Mevcut boya tabanlı oyunların görsel dilini kopyalama
- Silahla ateş edilen klasik shooter’a dönüşme
- Serbest çizim uğruna gamepad ve ağ desteğini feda etme
- Hikâye için maç akışını uzun sinematiklerle durdurma
- Üretilebilirlik kanıtlanmadan kullanıcı yapımı harita sistemi

---

# 49. AÇIK TASARIM SORULARI

Bu maddeler kesin değildir ve test gerektirir:

1. Figürlerde ayrı stamina/Tanım sistemi gerekli mi?
2. Ressam kamerası ne kadar ileriye bakabilmeli?
3. Figürün üstündeki bölgesel boya etkisi yeterince okunabilir mi?
4. Leke oyuncunun destek gücü ne kadar olmalı?
5. Suluboya akışı ne kadar deterministik olmalı?
6. Ressam rolleri bölüm içinde değiştirilebilmeli mi?
7. Figür aynı araçtan dört tane seçebilmeli mi?
8. Tuval Bütünlüğü sıfırında kesin tur sonucu ne olmalı?
9. Çerçeve durakları tempoyu kesiyor mu?
10. Harita günlük seed kullanmalı mı, yoksa haftalık mı?
11. Casual maç tek yarı hızlı seçenek sunmalı mı?
12. Ressamların serbest stroke’u gamepad’de ne kadar otomatik düzeltilmeli?
13. Figürlerin birbirini fiziksel itmesi griefing yaratıyor mu?
14. Mürekkep yaratığı kontrol kamerası ne kadar yakın olmalı?
15. Çıkışın sürekli görünmesi gizem ve keşfi azaltıyor mu?
16. Halka açık hızlı oyunun ana preset’i 2 Ressam–4 Figür mü, 4v4 mü olmalı?
17. Tek Ressamın iki disiplin arasında geçiş süresi ne kadar olmalı?
18. Rotation bireysel puanı oyuncuya kolay açıklanabiliyor mu?
19. Maksimum 10 oyunculu lobi üretim maliyetini haklı çıkarıyor mu?
20. Figür/Ressam oranı hangi eşiklerde yeni Atölye Bütçesi profili gerektiriyor?

---

# 50. VERTICAL SLICE KABUL KRİTERLERİ

Vertical slice ancak şu koşullarda başarılı kabul edilir:

## Oynanış

- 2 Ressam–4 Figür Atelier Escape grubu maçı tamamlayabiliyor ve yeniden oynamak istiyor.
- 4v4 testinde iki takım da rol değişiminden sonra oynamaya devam etmek istiyor.
- 1v1, 2v4 ve 4v4 kompozisyonlarında ana boya/karşı hamle kuralları değişmeden çalışıyor.
- Figürler her Ressam rolüne en az iki karşılık sayabiliyor.
- İki Ressam en az üç farklı kombinasyon kurabiliyor.
- Maç başına en az bir çizim Figürler tarafından avantaja çevriliyor.
- Leke oyuncuların çoğu oyundan kopmuyor.

## Teknik

- Hedef donanımda 60 FPS’e yakın stabilite.
- Stroke senkronizasyonunda belirgin kopma yok.
- Reconnect dünya durumunu doğru kuruyor.
- Tuval Bütünlüğü ve skor deterministik.
- Altı ve sekiz oyuncuda ağ tüketimi kabul edilebilir.
- On oyunculu maksimum lobi stres testi çökmeden tamamlanıyor.

## Görsel

- Dört malzeme iki saniyede ayırt ediliyor.
- Dev fırça ölçeği hissediliyor.
- Figürler yoğun boya içinde kaybolmuyor.
- Video klibinde mekanik açıklamasız anlaşılabiliyor.

## Ürün

- Test oyuncularının çoğu konsepti tek cümleyle doğru anlatabiliyor.
- İzleyiciler “Ressam olarak da oynamak istiyorum” diyor.
- Maçtan doğal paylaşılabilir anlar çıkıyor.

---

# 51. GLOSSARY / TERİMLER

| Terim | Açıklama |
|---|---|
| Figür | Tablonun içinde çıkışa ilerleyen oyuncu |
| Ressam | Tablonun dışından malzeme uygulayan oyuncu |
| Stroke / Darbe | Tek bir doğrulanmış fırça çizimi komutu |
| Netlik | Figürün beden/kimlik bütünlüğü |
| Leke | Netliği biten Figürün alternatif destek formu |
| Pigment | Ressam yetenek kaynağı |
| Tuval Bütünlüğü | Ortak dünyanın yapısal durumu |
| Çerçeve Durağı | Bölgeler arası kısa toparlanma alanı |
| Glyph / Sembol | Mürekkep yaratığına davranış veren işaret |
| Sahiplenme | Mürekkep yaratığını doğrudan kontrol etme |
| Kuruma | Yağlı boyanın aktif yumuşak hâlden statik hâle geçmesi |
| Curator | Tarafsız çevre ve encounter yöneticisi |
| Kompozisyon Gerilimi | Olası takım çapında büyük saldırı bütçesi |
| Atölye Bütçesi | Figür/Ressam oranına göre eylem kapasitesini ölçekleyen sistem |
| Palet Yükü | Az Ressamlı lobilerde bir oyuncunun ana ve ikincil disiplin erişimi |
| Expedition | Zorunlu rol değişimi olmayan tek yolculuk formatı |
| Rotation | Ressam koltuklarının turlar arasında oyuncular arasında döndüğü format |
| Frame Versus | İki eşit takımın aynı seed’de rol değiştirdiği 4v4 rekabet formatı |
| Seed | İki yarıda eşit koşulları üreten harita/olay tohumu |

---

# 52. KISA PITCH METNİ

PAINTED ALIVE, bir grup oyuncunun canlı bir tablodan kaçmaya çalıştığı ve karşılarındaki 1–4 Ressamın aynı tabloyu dışarıdan gerçek zamanlı olarak yeniden boyadığı esnek asimetrik bir multiplayer oyunudur. Lobi, 1–6 Figür ve 1–4 Ressam arasında kendi kompozisyonunu seçebilir; önerilen arkadaş formatı iki Ressama karşı dört Figür, rekabetçi format ise rol değişimli 4v4’tür. Yağlı Boya fiziksel duvarlar ve yollar oluşturur; Mürekkep çizilen sembollerden canlı yaratıklar üretir; Suluboya akıntıları ve yüzey davranışını değiştirir; Silgi dünyanın katmanlarını kaldırır ve restore eder. Figürler çizimleri keser, emer, kurutur, sabitler ve Ressam saldırılarını kendi yollarına dönüştürür. Oyunun özü nişan almaktan değil, rakibin yolculuğunu canlı olarak yeniden yazmaktan gelir.

---

# 53. DEĞİŞMEZ VİZYON ÖZETİ

Yeni bir özellik önerisi aşağıdaki maddelerden birini bozuyorsa reddedilmeli veya yeniden tasarlanmalıdır:

1. Oyun 1–6 Figür ve 1–4 Ressam destekleyen esnek asimetrik rekabettir.
2. Lobi kompozisyonu özel maçta oyunculara aittir; resmî preset’ler kontrollüdür.
3. 4v4 iki yarılı Frame Versus rekabetçi standarttır, oyunun tek modu değildir.
4. Ressamlar dünyanın malzemesini değiştirir; klasik silahla vurmaz.
5. Figürler aktif karşı hamleye sahiptir.
6. Çizilen boya fiziksel ve sistemik sonuç üretir.
7. Dört Ressam malzemesi birbirinden farklı ve kombinasyonludur; dört ayrı oyuncu gerektirmez.
8. Harita kesintisiz bir yolculuktur.
9. Ölüm uzun seyirci moduna dönüşmez; Leke sistemi oyuncuyu içeride tutar.
10. Kalıcı progression rekabetçi güç vermez.
11. Teknik uygulama kontrollü illüzyon kullanır; sınırsız simülasyon peşinde koşmaz.
12. Görsel stil dokunsal, özgün ve okunabilirdir.
13. Her maç oyuncuların anlatabileceği doğal bir hikâye üretmelidir.
14. Oyuncu sayısı ölçeklemesi ham can/hasar çarpanı yerine eylem ve alan kontrolü bütçesiyle yapılır.

---

# 54. SONRAKİ SOMUT ADIM

Tam üretime geçmeden önce yapılacak ilk oynanabilir yapı:

> Bir Yağlı Boya Ressamı, tek bir Figürün önüne ıslak boya duvarı çizer. Figür duvar yükselmeden geçebilir, Palet Bıçağıyla kesebilir, Sabitleyiciyle erkenden kurutup kırabilir veya duvarı rampa olarak kullanabilir. Beş dakikalık prototip, aynı etkileşimin en az üç farklı doğal sonuca ulaştığını göstermelidir.

Bu prototip eğlenceli değilse daha fazla harita, lore, kozmetik veya görsel kalite üretilmez. Önce ana etkileşim düzeltilir.

---

# 55. DEĞİŞİKLİK GÜNLÜĞÜ

## 0.2.0 — 15 Temmuz 2026

- Sabit 4v4 ürün tanımı kaldırıldı; 1–4 Ressam ve 1–6 Figür esnek yapı kilitlendi.
- 2 Ressam–4 Figür Atelier Escape önerilen arkadaş formatı olarak eklendi.
- 4v4 iki yarılı Frame Versus rekabetçi standart olarak korundu.
- Expedition, Rotation ve Frame Versus maç aileleri tanımlandı.
- Özel lobi kompozisyon kontrolleri ve resmî preset sınırları eklendi.
- Az Ressamlı maçlar için Palet Yükü sistemi oluşturuldu.
- Değişken oyuncu oranları için Atölye Bütçesi ve normalize skor kuralları eklendi.
- MVP, ağ hedefleri, yol haritası, matchmaking ve kabul kriterleri esnek lobiye göre güncellendi.

## 0.1.0 — 15 Temmuz 2026

- Ana yüksek konsept kaydedildi.
- İlk tasarımda 4v4 iki yarılı Versus yapısı ana format olarak kaydedildi; 0.2.0’da rekabetçi preset’e dönüştürüldü.
- Figür ve dört Ressam rolü ayrıntılandırıldı.
- Netlik/Leke, pigment ve Tuval Bütünlüğü tanımlandı.
- İlk harita makro akışı oluşturuldu.
- Unity teknik mimarisi ve kontrollü simülasyon yaklaşımı belirlendi.
- MVP, vertical slice kriterleri, riskler ve yapılmayacaklar listesi eklendi.
