# 開門查水表！ (FBI Open Up)
<a href="https://gfycat.com/genuineredindianhare" target="_blank"><img src="../demo/demo9.png" width="800" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片](../demo/demo9.mp4))<br>
- 此插件可依照原始角色，將她們轉變為小蘿莉<br>
- 支援替換模板角色，例如: 若將模板自訂為巨乳姊姊，就可以轉變功能為替換成大姊姊；將模板訂為三頭身並開啟ABMX設定，就能成為三頭身化功能<br>
- 可在Main Game、Studio、Maker和Free H內執行<br>
- 我置入了幾張過場圖片和動畫，作為娛樂效果

### 使用說明
![image](../demo/demo9-1.png)<br>
1. 功能觸發圖標為一紅色書包，位置紀錄如下
    - Studio: 位在「Add」→「女角色」
    - Maker: 位在正上方之「衣裝切換欄」的右側
    - Free H: 位在左上角的「服裝」子選單之中
    - MainGame主遊戲: 位在滑鼠中鍵暫停時的右排按鍵最下方
2. **短按**一次**啟動\關閉**功能，並**替換\倒回**場景內的**所有角色**<br>
(Studio內**長按**可啟動\關閉功能但**不變更現有角色**)
3. 若功能開啟，Studio和Maker載入人物時人物會自動被替換<br>
這包含Studio的Scene存檔載入也會套用
4. 計算邏輯為: **新數據 = 原始數據 + ((模板數據 - 原始數據) * Change Rate)**<br>
此運算會套用至身體和臉部的所有原生數值<br>
(大致上等於Maker中身體\臉部頁籤最下面的所有陳列數值)
5. ABMX功能沒有計算，只能全部覆蓋，功能需要於設定中開啟。<br>**此功能設計用來三頭身化**

### Configure設定說明
- Change rate: 原始人物向模板人物改變的比例<br>數值為0(不改變)~1(全改變)。
- Enable: 是否啟用插件。<br>這同時反映在遊戲中的紅色書包圖標之明暗狀態。<br>Studio和Maker如果在啟用狀態載入新人物，新人物將會直接被替換。
- Effect on ABMX: 啟用ABMX覆寫功能<br>若啟用會把模板的ABMX全覆蓋至對象，且會禁用回退功能。 
- Sample chara: 模板人物路徑。<br>留空白即可使用預設人物。<br>可傳入絕對路徑或相對路徑，如「UserData/chara/female/*.png」。
- Video path: FBI.mp4影片的路徑<br>預設路徑為「UserData/audio/FBI.mp4」
- Video volume: 影片音量<br>預設為0.04，請視喜好自行調整。

### ※注意事項※
1. 雖然目前有作主遊戲之功能，但並未完整測試，且沒有計畫再完善它<br>
**主遊戲功能請單純作為附加功能視之**
1. Free H內沒有過場插圖；主遊戲沒有人物加亮動畫
1. 模板角色可在Configuration Manager設定內更改，製作要點請見後述
1. **如果不想要FBI影片，請移除mp4檔案即可**

### 模板角色製作指南
- 請製作出一個你心目中100%的角色存檔，例如: 100%的蘿莉、100%的御姊
- 對於ABMX數據，開啟功能後模板的ABMX是**完全覆蓋**對象人物，不受Change Rate影響<br>ABMX請只用在特殊身形，**例如三頭身化**<br>製做普通的模板時請不要使用ABMX
- 建議扒光她所有的衣服和飾品，以降低存檔體積和降低電腦負擔

# 需求依賴
BepInEx **v5.0**<br>
BepisPlugins r13.0.1

# 安裝方式
- 將*.dll放至「BepInEx/plugins/jim60105」資料夾之下<br>
- 將*.mp4影片放至「UserData/audio」資料夾之下 (可選)

# 下載位置
[Latest Release](https://github.com/jim60105/KK/releases/latest "Latest Release")
