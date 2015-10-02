﻿# CM3D2.AlwaysColorChange.Plugin
パーツごとに好きに色変更できるようにするプラグインです。  
まだ実験段階です。  
**New!**マスクおよびノード消去も一応操作できるようになりました。

## 導入方法
#### 前提条件  
UnityInjectorが導入済みであること。

#### インストール  
Download ZIPボタンを押してzipファイルをダウンロードします。  
zipファイルの中にあるUnityInjectorフォルダを、CM3D2フォルダにコピーしてください。

#### 自分でコンパイルする場合  
自分でコンパイルする場合、UnityInjectorフォルダにソース（**CM3D2.SubScreenPlugin.cs**）をコピーした後、コマンドプロンプト等で  
`cd [UnityInjectorフォルダ]`
`C:\Windows\Microsoft.NET\Framework\v3.5\csc /t:library /lib:..\CM3D2x64_Data\Managed /r:UnityEngine.dll /r:UnityInjector.dll /r:Assembly-CSharp.dll /r:Assembly-CSharp-firstpass.dll /r:ExIni.dll CM3D2.AlwaysColorChangePlugin.cs`  
を実行してください。

## 機能概要
#### 対象シーン
* エディット
* 夜伽
* ダンス
* ADVパート

で動作する（はず）です。

#### 使用方法
###### メニュー操作
F12キーでメニューが開きます。  
キーアサインを変更したい場合は、ソースを編集・コンパイルしてください。  

![GUI](http://i.imgur.com/KxcD2Gh.jpg  "GUI")

パーツを選択し、RGBAをスライダーで変更後、適用ボタンで色が変更されるはずです。  
パーツの元の色によっては変わらないかもしれません。  
Aを1未満にすると、無理やりTransシェーダーを適用して透過します。  
**New!**maskItemのクリア、nodeの表示切り替えできるようになりました。  

#### 今後やりたいこと  
できればいいな、であり、実装できるとは限りません。
* マテリアルごとに変更できるように
* 保存および自動適用  
  パーツごとはMod/hoge.modとして出力できるといいな。  
  maskItem/node消去はプリセットとセットでとか？
* maskItemやnode消去を自由に操作（暫定対応）
* パーツ変更を認識して自動的用
* テクスチャ差し替え
* 複数メイド対応
* GUIをなんとかしたい・・・

#### 権利とか？
ソースの改変・再配布等は自由にしていただいてかまいません。  
むしろやりたいこと・それを超える機能を実装してくれる人歓迎。

##更新履歴
####0.0.0.2
* マテリアルごとに色変更できるように
* 適用ボタンを押さなくてもスライダー変更でリアルタイムに適用

####0.0.0.1
* 初版（実験版）
