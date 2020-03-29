<a rel="license" href="LICENSE.html"><img alt="創用 CC 授權條款" style="border-width:0" src="https://i.creativecommons.org/l/by-nc-sa/3.0/tw/88x31.png" /></a><br />本<span xmlns:dct="http://purl.org/dc/terms/" href="http://purl.org/dc/dcmitype/InteractiveResource" rel="dct:type">著作</span>係採用<a rel="license" href="LICENSE.html">創用 CC 姓名標示-非商業性-相同方式分享 3.0 台灣 授權條款</a>授權.

# Studio服裝卡選擇性載入插件<br>Studio Coordinate Load Option
![image](demo/demo1.gif)<br>

- Studio的服裝卡讀取處，多一個選項盤可以選擇性載入服裝<br>
- 飾品可選擇「取代模式」和「增加模式」<br>
(取代模式會複寫同欄位的飾品，而增加模式會往空欄位一直附加上去)
- 「鎖定頭髮飾品」可將頭髮飾品鎖定，使之不會受到清除和複寫<br>
- **將「鎖定頭髮飾品」以外的選項全勾，並使用飾品「取代模式」即會調用遊戲原始程式碼**<br>

目前確定支援Plugin:<br>
- Koikatu Overlay Mods v5.0.2
- Koikatu ABMX V3.3
- Koikatu More Accessories v1.0.6
- Koikatu MaterialEditor v1.8

目前確定不支援Plugin:<br>
- Koikatu HairAccessoryCustomizer

# Studio全是妹子插件<br>Studio All Girls Plugin
![image](demo/demo2.gif)<br>

- 將Studio SceneData內所有男性以女性讀入<br>
- 身體外型依照其原始數據女體化<br>
- 插件可從Configuration Manager關閉功能<br>

以此插件可以實現跨性別替換角色卡功能<br>
例: 讀取一般的男女Scene，將男角色替換成女角色，就變成了百合Scene!<br>

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
<a href="https://blog.maki0419.com/2019/05/koikatu-studio-reflect-fk-fix.html" target="_blank"><img src="demo/demo5-5.png" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片1](demo/demo5-1.mp4) [影片2](demo/demo5-2.mp4) )

- 原始的「FKにポーズを反映」功能會複寫身體FK+脖子FK+手指FK<br>
→ 改成了只會複寫身體FK，脖子FK和手指FK維持原樣
- 原始的「FK 首 個別參照」功能，是直接複製アニメ的脖子方向<br>
→ 改成了會複製真實方向。意即可以使用「首操作 カメラ」定位後，再按我的「->FK(首)」按鈕複製至脖子FK

# Studio文字插件<br>Studio Text Plugin
<a href="https://gfycat.com/frayedsecretiberianbarbel" target="_blank"><img src="demo/demo6-2.JPG" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片](demo/demo6.mp4))<br>
- 從「add→アイテム→2D効果→文字Text」加載，右側選中後在anim選單編輯<br>
- 文字物件可修改字體、大小、樣式、顏色、錨點位置、對齊(換行後顯示選項)<br>
- 可保存文字設定，以作為NewText的預設參數<br>

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

- 此工具可導出當前遊戲中已加載的BepInEx插件和IPA插件<br>
- 格式為**Json和CSV**<br>
- 適配IPALoaderX v1.2以上版本<br>
- 重新Enable後會立即倒出當前加載清單

# 開門查水表！<br>FBI Open Up！
<a href="https://gfycat.com/genuineredindianhare" target="_blank"><img src="demo/demo9.png" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片](demo/demo9.mp4))<br>
- 支援替換模板角色，例如: 
    - 若將模板自訂為巨乳姊姊，就可以轉變功能為替換成大姊姊
    - 將模板訂為三頭身(Chibi)並開啟ABMX設定，這就能成為三頭身變化功能。
- 可在Main Game、Studio、Maker和Free H內執行<br>
- 我置入了幾張過場圖片和動畫，作為娛樂效果

詳細說明請見 [另一篇Readme](KK_FBIOpenUp/README.md)。如果你想要使用，我很確定你需要閱讀它

# 角色Overlay隨服裝變換<br>Chara Overlays Based On Coordinate
<a href="https://youtu.be/kGwZ9aLSXZo" target="_blank"><img src="demo/demo10.gif" title="Click the image to watch full video"></a><br>
↑ 請點選圖片觀看完整影片 ↑ Click the image to watch full video! ↑  (備用載點: [影片](demo/demo10-1.mp4))<br>

- 讓所有角色Overlay(Iris、Face、Body Overlay)隨著服裝變更，反映在人物存檔(CharaFile)和服裝存檔(CoordinateFile)上<br>
- 此插件在「讀存」跟「切換服裝」時覆蓋Overlay，依賴KSOX運作<br>
- v1.2.0起支援資源重用，同樣的貼圖重複使用時只會佔一份空間<br>
- **產生的存檔可以和「無此插件的遊戲環境」相容**，此時KSOX儲存的Overlay會被載入<br>
(存檔時，當前套用的Overlay依然會儲存進去，並在無插件環境時被讀取出來)<br>
- v1.3.0起Iris Overlay可只覆蓋在單眼

### 注意事項:<br>
- 特別需求 **KKAPI v1.9.5 & Illusion Overlay Mods v5.1.1** 以上版本<br>
- **預設不啟用服裝存檔功能，請至Configuration Manager確認所有儲存設定**<br>
- 以下狀況會顯示警示訊息 (警示可關閉)
    - 存角色時**有Overlay未被儲存**
    - 存服裝時存入了「**全無Overlay**」狀態<br>(如果開啟了服裝Coordinate儲存功能，但是卻沒有存入任何角色Overlay，**就會發生如「清除角色Overlay」的效果**)
- 強烈建議**只在需要時開啟服裝儲存**功能
- v1.2.3後的版本產生的存檔不能在更舊的版本中讀取，請更新

# 存檔尺寸調整工具<br>PNG Capture Size Modifier
![image](demo/demo11.gif)<br>

- 可調角色存檔、服裝存檔、Studio存檔的拍照尺寸<br>
- 可調CharaMaker中角色、服裝檔案選擇器的顯示列數<br>
- 放大Studio SceneData選擇器的選中預覧<br>
- 給角色存檔、Studio存檔加上浮水印角標<br>

請至設定中調整這些功能<br>
因為改變了存檔圖片尺寸，**強烈建議不要禁用Studio SceneData浮水印**，以利區分存檔PNG和普通截圖PNG<br>
**產生的存檔可以和「無此插件的遊戲環境」相容**<br>

# Studio千佳替換器<br>Studio Chika Replacer
![image](demo/demo12.gif)<br>

- 一鍵把Studio內的所有女角色都換成千佳(預設角色)，並保留原始人物的身形數據<br>
- 或可自訂要用來替換的角色<br>
- 可只替換選中的角色<br>
- 用選擇方式來替換時，可替換男角色<br>

快捷鍵我故意設定得的很複雜，以免誤觸 (可在config修改)<br>
全替換: Enter + 右Shift + 左Shift + 左Ctrl<br>
選擇替換: '(單引號) + 右Shift + 左Shift + 左Ctrl<br>

# Studio角色光綁定視角<br>Studio Chara Light Linked To Camera
![image](demo/demo13.gif)<br>

- 將Studio角色光和視角間之旋轉值連動
- 鎖定狀態能隨著SceneData儲存

### 使用範例:<br>
調整角色光為「右側背光，左側是面光」然後鎖定<br>
則不論視角如何旋轉，都會維持是畫面右側背光

# Studio 雙螢幕<br>Studio Dual Screen
<a href="https://youtu.be/zrIIoW44bsQ" target="_blank"><img src="demo/demo14.png" title="Click the image to watch video"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch video! ↑  (備用載點: [影片](demo/demo14.mp4))<br>

**必需要有實體雙顯示器才能使用**<br>
這是為了在VMD錄屏的同時操作UI而設計的插件<br>
- 啟用Studio的第二顯示器功能
- UI只會顯示在主顯示畫面
- Frame會顯示在雙畫面
- VMD和KK_StudioCharaLightLinkedToCamera會作用在第二畫面

### 注意:
- **必需要有實體雙顯示器才能使用**
- 預設快捷鍵為「未設定」，到Config設定後才能使用
- 修改畫面設定(濾鏡等)需要再次觸發快捷鍵以進行畫面同步
- 已知問題: 啟用雙螢幕後F9截圖會造成無回應，請改用F11 (目前沒有計劃深入這部份)

# 需求依賴
- コイカツ！ ダークネス (Koikatu! Darkness)
- **BepInEx v5.0.1**<br>
- BepisPlugins r13.0.3<br>

# 安裝方式
- 參考壓縮檔結構，將文件放進「BepInEx/plugins/jim60105」資料夾之下<br>

# 下載位置
[Latest Release](https://github.com/jim60105/KK/releases/latest "Latest Release")
