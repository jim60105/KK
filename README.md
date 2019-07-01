# Studio服裝卡選擇性載入插件 (Studio Coordinate Load Option)
![image](https://github.com/jim60105/KK/raw/master/demo/demo1.gif)<br>

在Studio的服裝卡讀取那裡，多一個選項盤讓你可以選擇性載入服裝<br>
遇到任何問題，**請將選項盤全勾即會調用遊戲原始程式碼**<br>
目前確定支援Plugin:<br>
- Koikatu Overlay Mods v4.2.1
- Koikatu ABMX V3.1.1
- Koikatu More Accessories v1.0.5

# Studio全是妹子插件 (Studio All Girls Plugin)
![image](https://github.com/jim60105/KK/raw/master/demo/demo2.gif)<br>

這會將Studio SceneData內所有男性以女性讀入<br>
身體外型依照其原始數據女體化<br>
以此插件可以實現跨性別替換角色卡功能<br>
例: 讀取一般的男女Scene，將男角色替換成女角色，就變成了百合Scene!<br>

### **警語**:<br>
1. 所有角色將以女性載入<br>
1. 此插件所產生之存檔，**所有角色皆會以女性存檔**<br>
1. POSE解鎖性別限制，男女都可讀取，寫入以女性寫入<br>

# Studio女體單色化插件 (Studio Simple Color On Girls)
![image](https://github.com/jim60105/KK/raw/master/demo/demo3.gif)<br>

使女性支持單色化功能，用意在於彌補全女插件所造成的限制<br>
可以和全女插件分開使用<br>
**依賴Darkness特典，0201版用戶請下載[舊版v1.0.1](https://github.com/jim60105/KK/releases/download/v19.05.16.2/OLD_KK_StudioSimpleColorOnGirls1.0.1.rar)版**<br>

# Studio換人插件 (Studio Chara Only Load Body)
![image](https://github.com/jim60105/KK/raw/master/demo/demo4.gif)<br>

保留衣服和飾品，只替換人物<br>
目前確定支援Plugin:<br>
- Koikatu Overlay Mods v4.2.1
- Koikatu More Accessories v1.0.5
- Koikatu KKPE v1.2.0
- Koikatu KK_UncensorSelector v3.6.4
- Koikatu KKABMX v3.1.1

# Studio IK→FK修正插件 (Studio Reflect FK Fix)
<a href="https://blog.maki0419.com/2019/05/koikatu-studio-reflect-fk-fix.html" target="_blank"><img src="https://github.com/jim60105/KK/raw/master/demo/demo5-5.png" width="800" title="Click the image to watch demo"></a><br>
↑ 請點選圖片觀看範例影片 ↑ Click the image to watch demo! ↑  (備用載點: [影片1](https://github.com/jim60105/KK/raw/master/demo/demo5-1.mp4) [影片2](https://github.com/jim60105/KK/raw/master/demo/demo5-2.mp4) )
### **修改兩個功能:**
- 原始的「FKにポーズを反映」功能會複寫身體FK+脖子FK+手指FK<br>
→ 改成了只會複寫身體FK，脖子FK和手指FK維持原樣
- 原始的「FK 首 個別參照」功能，是直接複製アニメ的脖子方向<br>
→ 改成了會複製真實方向。意即可以使用「首操作 カメラ」定位後，再按我的「->FK(首)」按鈕複製至脖子FK

# Studio文字插件 (Studio Text Plugin)
![image](https://github.com/jim60105/KK/raw/master/demo/demo6.gif)<br>

從「add→アイテム→2D効果→文字Text」加載，右側選中後在anim選單編輯<br>
建議分享Scene時一併分享使用的Fonts (It is recommended to share the Fonts used when sharing Scene.)<br>

### 注意事項:<br>
- Fonts會列出OS內安裝，支援Unity動態生成的所有字體<br>
- 若Scene保存後，在其他沒有安裝此Font的OS讀取，會加載MS Gothic<br>
- Color選取使用右下角遊戲原生Color選擇器<br>
- 文字重疊時偶爾會渲染不正確，這是Unity的問題，似乎無解<br>

# 需求依賴
BepInEx v4.1.1<br>
BepisPlugins r10.1

# 安裝方式
- 將所有的「*.dll」檔案放進BepInEx資料夾<br>

# 下載位置
[Latest Release](https://github.com/jim60105/KK/releases/latest "Latest Release")
