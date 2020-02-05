# Studio服裝卡選擇性載入插件<br>Studio Coordinate Load Option
![image](demo/demo1.gif)<br>

在Studio的服裝卡讀取那裡，多一個選項盤讓你可以選擇性載入服裝<br>
飾品可選擇「取代模式」和「增加模式」<br>
取代模式會複寫同欄位的飾品，而增加模式會往空欄位一直附加上去<br>
「鎖定頭髮飾品」可將頭髮飾品鎖定，使之不會受到清除和複寫<br>
**將「鎖定頭髮飾品」以外的選項全勾，並使用飾品「取代模式」即會調用遊戲原始程式碼**<br>
目前確定支援Plugin:<br>
- Koikatu Overlay Mods v5.0.2
- Koikatu ABMX V3.3
- Koikatu More Accessories v1.0.6
- Koikatu MaterialEditor v1.8
- ~~Koikatu HairAccessoryCustomizer v1.1.2~~ ((v3.1.0起支援他，但是v3.1.0現在依然BUG中))

# Studio全是妹子插件<br>Studio All Girls Plugin
![image](demo/demo2.gif)<br>

這會將Studio SceneData內所有男性以女性讀入<br>
身體外型依照其原始數據女體化<br>
以此插件可以實現跨性別替換角色卡功能<br>
例: 讀取一般的男女Scene，將男角色替換成女角色，就變成了百合Scene!<br>
插件可從Configuration Manager關閉功能<br>

### **警語**:<br>
1. 所有角色將以女性載入<br>
1. 此插件所產生之存檔，**所有角色皆會以女性存檔**<br>
1. POSE解鎖性別限制，男女都可讀取，寫入以女性寫入<br>

# Studio女體單色化插件<br>Studio Simple Color On Girls
![image](demo/demo3.gif)<br>

使女性支持單色化功能，用意在於彌補全女插件所造成的限制<br>
可以和全女插件分開使用<br>
**依賴Darkness特典，無Darkness必定出問題**<br>

# Studio換人插件<br>Studio Chara Only Load Body
![image](demo/demo4.gif)<br>

保留衣服和飾品，只替換人物<br>
目前確定支援Plugin:<br>
- Koikatu Overlay Mods v5.0.2
- Koikatu More Accessories v1.0.7
- Koikatu KK_UncensorSelector v3.8.3
- Koikatu KKABMX v3.3
- Koikatu Chara Overlays Based On Coordinate v1.1.0 <br>(Chara Overlays跟著插件了，如果要更改載入與否請至設定修改)

# Studio IK→FK修正插件<br>Studio Reflect FK Fix
<a href="https://blog.maki0419.com/2019/05/koikatu-studio-reflect-fk-fix.html" target="_blank"><img src="demo/demo5-5.png" width="800" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片1](demo/demo5-1.mp4) [影片2](demo/demo5-2.mp4) )
### **修改兩個功能:**
- 原始的「FKにポーズを反映」功能會複寫身體FK+脖子FK+手指FK<br>
→ 改成了只會複寫身體FK，脖子FK和手指FK維持原樣
- 原始的「FK 首 個別參照」功能，是直接複製アニメ的脖子方向<br>
→ 改成了會複製真實方向。意即可以使用「首操作 カメラ」定位後，再按我的「->FK(首)」按鈕複製至脖子FK

# Studio文字插件<br>Studio Text Plugin
<a href="https://gfycat.com/frayedsecretiberianbarbel" target="_blank"><img src="demo/demo6-2.JPG" width="800" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片](demo/demo6.mp4))<br>
從「add→アイテム→2D効果→文字Text」加載，右側選中後在anim選單編輯<br>
文字物件可修改字體、大小、樣式、顏色、錨點位置、對齊(換行後顯示選項)<br>
可保存文字設定，以作為NewText的預設參數<br>
建議分享Scene時一併分享使用的Fonts (It is recommended to share the Fonts used when sharing Scene.)<br>

### 注意事項:<br>
- Fonts會列出OS內安裝，支援Unity動態生成的所有字體，字體總數在500以下時可以顯示預覽<br>
- 若Scene保存後，在其他沒有安裝此Font的OS讀取，會加載MS Gothic<br>
- Color選取使用右下角遊戲原生Color選擇器<br>
- 文字中插入換行符「\n」可以換行，插入換行符後會顯示「對齊」編輯選項<br>
- 文字重疊時偶爾會渲染不正確，這是Unity的問題，似乎無解<br>

# Studio自動關閉Scene載入視窗<br>Studio Auto Close Loading Scene Window
![image](demo/demo7.png)<br>

Load Scene視窗處，在Import或Load後自動關閉視窗<br>
可以使用Configuration Manager個別設定Import/Load是否啟用 (預設皆啟用)<br>

# 插件清單工具<br>Plugin List Tool
![image](demo/demo8.png)<br>

此工具可導出當前遊戲中已加載的BepInEx插件和IPA插件<br>
格式為**Json和CSV**<br>
適配IPALoaderX v1.2以上版本<br>
重新Enable後會立即倒出當前加載清單

# 開門查水表！<br>FBI Open Up！
<a href="https://gfycat.com/genuineredindianhare" target="_blank"><img src="demo/demo9.png" width="800" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片](demo/demo9.mp4))<br>
此插件可依照原始角色，將她們轉變為小蘿莉<br>
可在Studio、Maker和Free H內執行<br>
支援替換模板角色，例如: 若將模板自訂為巨乳姊姊，就可以轉變功能為替換成大姊姊<br>
詳細說明請見: [另一篇Readme](KK_FBIOpenUp/README.md)

# 角色Overlay隨服裝變換<br>Chara Overlays Based On Coordinate
<a href="https://youtu.be/kGwZ9aLSXZo" target="_blank"><img src="demo/demo10.gif" width="800" title="Click the image to watch full video"></a><br>
↑ 請點選圖片觀看完整影片 ↑ Click the image to watch full video! ↑  (備用載點: [影片](demo/demo10-1.mp4))<br>

讓所有角色Overlay(Iris、Face、Body Overlay)隨著服裝變更，反映在人物存檔和服裝存檔上<br>
此插件在「讀存」跟「切換服裝」時覆蓋Overlay，依賴KSOX運作<br>
**產生的存檔可以和「無此插件的遊戲環境」相容**，最後KSOX儲存的Overlay會被載入<br>
(存檔時，當前套用的Overlay依然會儲存進去，並在無插件環境時被讀取出來)<br>
v1.2.0起支援資源重用，同樣的貼圖重複使用時只會佔一份空間<br>

### 注意事項:<br>
- 特別需求 **KKAPI v1.9.5 & Illusion Overlay Mods v5.1.1** 以上版本<br>
- **預設不啟用服裝存檔功能，請至Configuration Manager確認所有儲存設定**<br>
- 以下狀況會顯示警示訊息 (警示可關閉)
    - 存角色時**有Overlay未被儲存**
    - 存服裝時存入了「**全無Overlay**」狀態<br>(如果開啟了服裝Coordinate儲存功能，但是卻沒有存入任何角色Overlay，**就會發生如「清除角色Overlay」的效果**)
- 強烈建議**只在需要時開啟服裝儲存**功能

# 需求依賴
**BepInEx v5.0.1**<br>
BepisPlugins r13.0.3

# 安裝方式
- 參考壓縮檔結構，將文件放進「BepInEx/plugins/jim60105」資料夾之下<br>

# 下載位置
[Latest Release](https://github.com/jim60105/KK/releases/latest "Latest Release")
