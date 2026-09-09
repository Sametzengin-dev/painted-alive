# Painted Alive M55.1.1.0.1 — Nested Assets Path Repair

Bu hotfix yalnızca yanlışlıkla oluşan `Assets/Assets/_Project/...` klasör yapısını düzeltir. Gameplay/runtime semantiğini değiştirmez.

## Neden gerekli?
Stack trace içindeki şu yol hatayı doğruluyor:

`Assets/Assets/_Project/Code/Editor/SetupFigureMixamoProductionMilestone_M55_1_1.cs`

M55.1.1 installer ise kaynakları doğru olarak `Assets/_Project/...` altında arıyor.

## Kurulum
1. Unity'yi kapatmak zorunda değilsin.
2. Bu ZIP'i **projenin kök klasörüne** çıkar. Yani ZIP içindeki `Assets` klasörü, projendeki mevcut `Assets` klasörüyle merge olmalı. **ZIP'i mevcut `Assets` klasörünün içine çıkarma.**
3. Unity compile bittikten sonra:
   `Tools > Painted Alive > Milestones > 55.1.1 - Repair Nested Assets Folder`
4. Repair sonucu `Move failures: 0` olmalı.
5. Unity yeniden compile/import yaptıktan sonra:
   `Tools > Painted Alive > Milestones > 55.1.1 - Install Production Figure + Mixamo Locomotion`
6. Ardından:
   `55.1.1 - Diagnose Production Figure + Mixamo Locomotion`

## Beklenen sonuç
Kaynaklar artık şu yapıda olmalı:

`Assets/_Project/Art/Models/Figures/Source/PA_Figure_Rigged.fbx`

`Assets/_Project/Art/Textures/Figures/...`

`Assets/_Project/Art/Animations/Figure/Locomotion/...`

`Assets/Assets/_Project` yanlış klasörü kaldırılmış olmalı (başka dosya yoksa).
