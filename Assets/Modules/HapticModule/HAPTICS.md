# Haptic Feedback

## Ayarlar

`Assets/Modules/HapticModule/Scriptables/CD_Haptic.asset` dosyasını seç.

- **Default Enabled:** İlk kurulumda titreşimin açık olup olmadığı. Kayıtlı oyuncu tercihini değiştirmez.
- **Presets / Preset:** Oyunun istediği geri bildirim.
- **Effect:** Bu istek geldiğinde çalacak cihaz efekti. Örneğin birleşmeyi daha hafif yapmak için Medium satırının Effect alanını Light yap.
- **Enabled:** Sadece o geri bildirimi kapatır.
- **Minimum Interval:** Aynı presetin tekrar çalması arasındaki minimum süre (saniye).
- **Log In Editor:** Unity Console'da kabul edilen titreşim isteklerini gösterir. Editor fiziksel titreşim üretmez.

| Olay | Preset |
|---|---|
| Jöleyi tutma | Selection |
| Bırakınca birleşme | Medium |
| Normal kapı çıkışı | Heavy |
| Son jölenin çıkışı / kazanma | Success |
| Sürenin bitmesi / kaybetme | Failure |

Sürükleme kareleri, yalnızca temas, Undo ve Restart titreşim üretmez. Son çıkışta sadece Success çalar.

Oyuncu **Ayarlar → Titreşim: Açık/Kapalı** ile açıp kapatabilir. Tercih `Haptic.Enabled` anahtarında saklanır. Açarken Selection örneği çalar.

## Sahne kurulumu

`Prefabs/HapticServiceRoot.prefab` sahnede bir kez bulunmalı. Initialize Order: **-35**. Kendi RootAdapter / Scriptables alanında `CD_Haptic` tanımlıdır. Oyun ve ayarlar test sahneleri bağlandı. Açık MainScene değişikliği kullanıcı tarafından kaydedilmelidir; proje kuralı açık sahneyi otomatik kaydetmeyi yasaklar.

## Kontrol

- `zTestModules/HapticTestModule/Scenes/HapticTestScene.unity`: Play ile 12 config/cooldown kontrolü.
- MainScene: tut, sürükle, bırakıp birleştir; normal çıkışı ve son çıkışı karşılaştır; sürenin bitmesini dene.
- Ayarı kapat, uygulamayı yeniden aç: kapalı kalmalı. Açınca örnek titreşim gelmeli.
- Ayar ekranını birkaç kez aç/kapat: tek basış tek değişiklik yapmalı.
- Android ve iOS cihazda dene. Cihazın sistem titreşim ayarı kapalıysa geri bildirim gelmeyebilir; efekt hissi donanıma göre değişir.

Android script derlemesi ayrı doğrulanır; bu APK/IL2CPP ya da gerçek telefon testi yerine geçmez. iOS native derlemesi için Mac/Xcode gerekir.
