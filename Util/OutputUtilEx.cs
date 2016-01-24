﻿/*
 * OutputUtilラッパー
 * カスメ専用クラス等を使うメソッドを拡張するユーティリティ
 */
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Windows.Forms.VisualStyles;
using UnityEngine;
using CM3D2.AlwaysColorChangeEx.Plugin.Data;

namespace CM3D2.AlwaysColorChangeEx.Plugin.Util
{
    /// <summary>
    /// Description of OutputUtilEx.
    /// </summary>
    public sealed class OutputUtilEx
    {
        private static OutputUtilEx instance = new OutputUtilEx();
        
        public static OutputUtilEx Instance {
            get {
                return instance;
            }
        }
        private static readonly OutputUtil util = OutputUtil.Instance;
        
        private OutputUtilEx() { }

        public string GetModDirectory() {
            return util.GetModDirectory();
        }

        public string GetACCDirectory() {
            return util.GetACCDirectory();
        }

        public string GetExportDirectory() {
            return util.GetACCDirectory("Export");
        }

        public string GetACCDirectory(string subName) {
            return util.GetACCDirectory(subName);
        }

        public void WriteBytes(string file, byte[] imageBytes) {
            util.WriteBytes(file, imageBytes);
        }

        public void WriteTexFile(string filepath, string txtPath, byte[] imageBytes) {
            util.WriteTex(filepath, txtPath, imageBytes);
        }
        public void WritePmat(string outpath, string name, float priority, string shader) {
            util.WritePmat(outpath, name, priority, shader);
        }

        // infile,outfileで、ファイルが特定できる必要あり
        public void Copy(string infilepath, string outfilepath) {
            util.Copy(infilepath, outfilepath);
        }

        public bool Exists(string filename) {
            using (AFileBase aFileBase = global::GameUty.FileOpen(filename)) {
                if (!aFileBase.IsValid()) {
                    return false;
                }
            }
            return true;
        }

        // 外部DLL依存
        // 一旦バイト配列にロードすることなくStreamオブジェクトとして参照可能とする
        public Stream GetStream(string filename) {
            try {
                AFileBase aFileBase = global::GameUty.FileOpen(filename);
                if (!aFileBase.IsValid()) {
                    var msg = "指定ファイルが見つかりません。file="+ filename;
                    LogUtil.ErrorLog(msg);
                    throw new ACCException(msg);
                }
                //new MemoryStream(aFileBase.ReadAll()
                return new FileBaseStream(aFileBase);
            } catch (Exception e) {
                var msg = "指定ファイルが読み込めませんでした。"+ filename+ e.Message;
                LogUtil.ErrorLog(msg, e);
                throw new ACCException(msg, e);
            }
        }

        public byte[] LoadInternal(string filename) {
            try {
                using (AFileBase aFileBase = global::GameUty.FileOpen(filename)) {
                    if (!aFileBase.IsValid()) {
                        var msg = "指定ファイルが見つかりません。file="+ filename;
                        LogUtil.ErrorLog(msg);
                        throw new ACCException(msg);
                    }
                    return aFileBase.ReadAll();
                }
            } catch (Exception e) {
                var msg = "指定ファイルが読み込めませんでした。"+ filename+ e.Message;
                LogUtil.ErrorLog(msg, e);
                throw new ACCException(msg, e);
            }
        }
        public Texture2D LoadTexture(string filename) {
            byte[] data = ImportCM.LoadTexture(filename);
            var tex2d = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex2d.LoadImage(data);
            tex2d.name = Path.GetFileNameWithoutExtension(filename);
            tex2d.wrapMode = TextureWrapMode.Clamp;
            return tex2d;
        }

        // 外部DLL依存
        public void Copy(AFileBase infile, string outfilepath) {
            const int buffSize = 8196;
            using ( var writer = new BinaryWriter(File.OpenWrite(outfilepath)) )  {

                var buff = new byte[buffSize];
                int length = 0;
                while ((length = infile.Read(ref buff, buffSize)) > 0) {
                    writer.Write(buff, 0, length);
                }
            }
        }

        public List<ReplacedInfo> WriteMenuFile(string infile, string outfilepath, ResourceRef res) {
            using ( var writer = new BinaryWriter(File.OpenWrite(outfilepath)) )
                using ( var reader = new BinaryReader(GetStream(infile), Encoding.UTF8) ) {
                
                try {
                    util.TransferMenu(reader, writer, res.EditTxtPath(), res.ReplaceMenuFunc());
                    return res.replaceFiles;
                } catch(Exception e) {
                    var msg = LogUtil.ErrorLog("menuファイルの作成に失敗しました。 file=", outfilepath, e);
                    throw new ACCException(msg.ToString(), e);
                }
            }
        }

        // 外部DLL依存
        public void TransferModel(AFileBase infile, string outfilepath) {
            using ( var writer = new BinaryWriter(File.OpenWrite(outfilepath)) )
                using ( var reader = new BinaryReader(new MemoryStream(infile.ReadAll()), Encoding.UTF8)) {
                util.CopyModel(reader, writer, null);
            }
        }

        ///
        ///
        public bool WriteModelFile(string infile, string outfilepath, SlotMaterials slotMat) {
            using ( var writer = new BinaryWriter(File.OpenWrite(outfilepath)) )
                using ( var reader = new BinaryReader(GetStream(infile), Encoding.UTF8) ) {
                return TransferModel(reader, writer, slotMat);
            }
        }

        public bool TransferModel(BinaryReader reader, BinaryWriter writer, SlotMaterials slotMat) {
            // ヘッダ
            string head = reader.ReadString();
            if (head != FileConst.HEAD_MODEL) {
                var msg = LogUtil.ErrorLog("正しいモデルファイルではありません。ヘッダが不正です。", head);
                throw new ACCException(msg.ToString());
            }
            writer.Write(head);
            writer.Write(reader.ReadInt32());  // ver
            writer.Write(reader.ReadString()); // "_SM_" + name
            writer.Write(reader.ReadString()); // base_bone
            int count = reader.ReadInt32();
            writer.Write(count);  // num (bone_count)
            for(int i=0; i< count; i++) {
                writer.Write(reader.ReadString());
                writer.Write(reader.ReadByte());
            }

            for(int i=0; i< count; i++) {
                int count2 = reader.ReadInt32();
                writer.Write(count2);
            }

            for(int i=0; i< count; i++) {
                // (x, y, z), (x2, y2, z2, w)
                TransferVec(reader, writer, 7);
            }
            int vertexCount = reader.ReadInt32();
            int facesCount = reader.ReadInt32();
            int localBoneCount = reader.ReadInt32();
            writer.Write(vertexCount);
            writer.Write(facesCount);
            writer.Write(localBoneCount);

            for(int i=0; i< localBoneCount; i++) {
                writer.Write(reader.ReadString());
            }
            for(int i=0; i< localBoneCount; i++) {
                TransferVec(reader, writer, 16);
            }
            for(int i=0; i< vertexCount; i++) {
                TransferVec(reader, writer, 8);
            }
            int vertexCount2 = reader.ReadInt32();
            writer.Write(vertexCount2);
            for(int i=0; i< vertexCount2; i++) {
                TransferVec4(reader, writer);
            }
            for(int i=0; i< vertexCount; i++) {
                for (int j=0; j< 4; j++) {
                    writer.Write(reader.ReadUInt16());
                }
                TransferVec4(reader, writer);
            }
            for(int i=0; i< facesCount; i++) {
                int cnt = reader.ReadInt32();
                writer.Write(cnt);
                for (int j=0; j< cnt; j++) {
                    writer.Write(reader.ReadInt16());
                }
            }
            // material
            int mateCount = reader.ReadInt32();
            writer.Write(mateCount);
            for(int matNo=0; matNo< mateCount; matNo++) {
                var tm = slotMat.Get(matNo);
                TransferMaterial(reader, writer, tm, tm.onlyModel);
            }
            
            // morph
            while (true) {
                string name = reader.ReadString();
                writer.Write(name);
                if (name == "end") break;

                if (name == "morph") {
                    string key = reader.ReadString();
                    writer.Write(key);
                    int num = reader.ReadInt32();
                    writer.Write(num);
                    for (int i=0; i< num; i++) {
                        writer.Write(reader.ReadUInt16());
                        // (x, y, z), (x, y, z)
                        TransferVec(reader, writer, 6);
                    }
                }
            }
            return true;
        }
        public void WriteMateFile(string infile, string outfilepath, TargetMaterial trgtMat) {
            using ( var writer = new BinaryWriter(File.OpenWrite(outfilepath)) )
                using ( var reader = new BinaryReader(GetStream(infile), Encoding.UTF8) ) {
                
                writer.Write(reader.ReadString()); // ヘッダ (CM3D2_MATERIAL)
                writer.Write(reader.ReadInt32());  // バージョン
                writer.Write(reader.ReadString()); // マテリアル名1

                TransferMaterial(reader, writer, trgtMat, true);
            }
        }
        // modelファイル内のマテリアル情報であり、通常のmaterialファイルのheader, version, name1は存在しない
        public void TransferMaterial(BinaryReader reader, BinaryWriter writer, TargetMaterial trgtMat, bool overwrite) {

            // マテリアル名
            reader.ReadString();
            writer.Write(trgtMat.editname);

            string shaderName1 = reader.ReadString();
            string shaderName2 = reader.ReadString();
            if (trgtMat.shaderChanged) {
                shaderName1 = trgtMat.ShaderNameOrDefault(shaderName1);
                shaderName2 = ShaderMapper.GatShader2(shaderName1);
            }
            writer.Write(shaderName1);
            writer.Write(shaderName2);

            var matType = trgtMat.editedMat.type;
            var writed = new HashSet<string>();
            while(true) {
                string type = reader.ReadString();
                //writer.Write(type);
                if (type == "end") break;

                string propName = reader.ReadString();
                if (!matType.IsValidProp(propName)) {
                    // シェーダに対応していないプロパティは読み捨て
                    DiscardMateProp(reader, type);
                    continue;
                }
                
                if (!overwrite) { 
                    // .mateからマテリアル変更で書き換えるため、そのまま転送
                    // ただし、model上に記述されたマテリアルで指定されたtexファイルは存在する必要あり
                    TransferMateProp(reader, writer, type, propName);
                } else {
                    switch (type) {
                    case "tex":
                        // .mateによるマテリアル変更がないケースのみ書き換える
                        // 
                        // texプロパティがある場合にのみ設定
                        TargetTexture trgtTex = null;
                        trgtMat.texDic.TryGetValue(propName, out trgtTex);
                        if (trgtTex == null || trgtTex.tex == null || trgtTex.fileChanged || trgtTex.colorChanged) {
                            // 変更がある場合にのみ書き換え (空のものはnull指定)
                            trgtTex.worksuffix = trgtMat.worksuffix;
                            string srcfile = null;
                            TransferMateProp(reader, null, type, null, ref srcfile);
                            if (trgtTex != null) trgtTex.workfilename = srcfile;

                            WriteTex(writer, propName, trgtMat, trgtTex);
                        } else {
                            // 変更がないものはそのまま転送
                            TransferMateProp(reader, writer, type, propName);
                        }
                        break;
                    case "col":
                    case "vec":
                        Write(writer, type, propName);
                        Write(writer, trgtMat.editedMat.material.GetColor(propName));
                        
                        DiscardMateProp(reader, type);
                        break;
                    case "f":
                        Write(writer, type, propName);
                        Write(writer, trgtMat.editedMat.material.GetFloat(propName));
                        
                        DiscardMateProp(reader, type);
                        break;
                    }
                }
                writed.Add(propName);
            }

            // シェーダで設定されるプロパティ数が一致しない場合、不足propを追記
            if (matType.propNameSet.Count != writed.Count) {
                foreach (var name in matType.propNameSet) {
                    var propName = name.ToString();
                    if (writed.Contains(propName)) continue;

                    // prop追記
                    PropType type = ShaderMapper.GetType(name);
                    switch(type) {
                        case PropType.tex:
                            TargetTexture trgtTex = null;
                            trgtMat.texDic.TryGetValue(propName, out trgtTex);
                            WriteTex(writer, propName, trgtMat, trgtTex);
                            break;
                        case PropType.col:
                            Write(writer, type.ToString(), propName);
                            Write(writer, trgtMat.editedMat.material.GetColor(propName));
                            break;
                        case PropType.f:
                            Write(writer, type.ToString(), propName);
                            Write(writer, trgtMat.editedMat.material.GetFloat(propName));
                            break;
                    }
                }
            }

            writer.Write("end");
        }

        private void WriteTex(BinaryWriter writer, string propName, TargetMaterial tm, TargetTexture trgtTex) {
            Write(writer, "tex");
            Write(writer, propName);

            var sub = "tex2d";
            if (trgtTex == null || trgtTex.tex == null)  {
                sub = "null";
            }
            Write(writer, sub);
            switch (sub) {
            case "tex2d":
                // カラー変更時にはファイル生成するため、ファイル名も変更が必要
                if (trgtTex.fileChanged || trgtTex.colorChanged) {
                    Write(writer, trgtTex.EditFileNameNoExt()); // 拡張子不要
                    //Write(writer, trgtTex.EditFileName());
                    Write(writer, trgtTex.EditTxtPath());

                    Write(writer, tm.editedMat.material.GetTextureOffset(propName));
                    Write(writer, tm.editedMat.material.GetTextureScale(propName));
                }
                break;
            case "null":
                break;
            case "texRT":            // texRTはない
                writer.Write("");
                writer.Write("");
                break;
            }
        }

        private void DiscardMateProp(BinaryReader reader, string type) {
            TransferMateProp(reader, null, type, null);
        }
        private void TransferMateProp(BinaryReader reader, BinaryWriter writer, string type, string propName, ref string texfile) {
            Write(writer, type, propName);
            switch (type) {
                case "tex":
                    string sub = reader.ReadString();
                    Write(writer, sub);
                    switch (sub) {
                        case "tex2d":
                            var file = reader.ReadString();
                            texfile = file;
                            string txtpath = reader.ReadString();
                            Write(writer, file, txtpath);
                            TransferVec4(reader, writer);
                            break;
                        case "null":
                            break;
                        case "texRT":
                            TransferString(reader, writer, 2);
                            break;
                    }
                    break;
                case "col":
                case "vec":
                    TransferVec4(reader, writer);
                    break;
                case "f":
                    Write(writer, reader.ReadSingle());
                    break;
            }
        }

        private void TransferMateProp(BinaryReader reader, BinaryWriter writer, string type, string propName) {
            string file = null;
            TransferMateProp(reader, writer, type, propName, ref file);
        }
        
        private void Write(BinaryWriter writer, params string[] data) {
            if (writer == null) return;
            foreach (var d in data)  writer.Write(d);
        }
        private void Write(BinaryWriter writer, float data) {
            if (writer != null) writer.Write(data);
        }
        private void Write(BinaryWriter writer, byte data) {
            if (writer != null) writer.Write(data);
        }
        public void Write(BinaryWriter writer, Vector2 data) {
            if (writer == null) return;
            writer.Write(data.x);
            writer.Write(data.y);
        }
        private void Write(BinaryWriter writer, params float[] data) {
            if (writer == null) return;
            foreach (float f in data)  writer.Write(f);
        }
        public void Write(BinaryWriter writer, Color color) {
            if (writer == null) return;
            writer.Write(color.r);
            writer.Write(color.g);
            writer.Write(color.b);
            writer.Write(color.a);
        }
        private void TransferVec(BinaryReader reader, BinaryWriter writer, int size) {
            for(int i=0; i<size; i++) {
                var data = reader.ReadSingle();
                if (writer != null) writer.Write(data);
            }
        }
        private void TransferVec3(BinaryReader reader, BinaryWriter writer) {
            for(int i=0; i<3; i++) {
                var data = reader.ReadSingle();
                if (writer != null) writer.Write(data);
            }
        }
        private void TransferVec4(BinaryReader reader, BinaryWriter writer) {
            for(int i=0; i<4; i++) {
                var data = reader.ReadSingle();
                if (writer != null) writer.Write(data);
            }
        }
        private void TransferString(BinaryReader reader, BinaryWriter writer, int count) {
            for(int i=0; i<count; i++) {
                var data = reader.ReadString();
                if (writer != null) writer.Write(data);
            }
        }

        public void CopyTex(string infile, string outfilepath, string txtpath, TextureModifier.FilterParam filter) {

            // テクスチャをロードし、フィルタを適用
            var srcTex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            srcTex.LoadImage( ImportCM.LoadTexture(infile) );
            Texture2D dstTex;
            dstTex = (filter != null) ? ACCTexturesView.Filter(srcTex, filter) : srcTex;

            WriteTexFile(outfilepath, txtpath, dstTex.EncodeToPNG());
        }
    }
}