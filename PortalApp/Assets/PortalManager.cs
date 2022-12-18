using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//ステンシルの比較条件を操作する際に必要
using UnityEngine.Rendering;
//ClippingPlaneをこのスクリプトで操作するためのMRTKの関連機能を読み込み
using Microsoft.MixedReality.Toolkit.Utilities;
public class PortalManager : MonoBehaviour
{
    //ClippingPlaneスクリプト
    [SerializeField] ClippingPlane clippingPlane;
    //移動先の世界の3Dモデルをまとめたオブジェクト
    [SerializeField] GameObject worldObject;
    //上記オブジェクトのマテリアル(描画設定ファイル)を保持するために使用
    List<Material> worldMaterials = new List<Material>();
    //現在の表示モード
    bool isARMode = true;
    //ゲートに表と裏どちらから入るか (1:表側から入る -1:裏側から入る)
    float enteringSide;
    // Start is called before the first frame update
    void Start()
    {
        //移動先の3DモデルのRendererを取得
        Renderer[] renderers = worldObject.GetComponentsInChildren<Renderer>();
        foreach(Renderer renderer in renderers)
        {
            //clippingPlaneにWorld内のオブジェクトを登録(必ず先に実行すること)
            clippingPlane.AddRenderer(renderer);
            //マテリアルを取得
            Material material = renderer.sharedMaterial;
            //既に他のオブジェクトから取得したマテリアルでなければリストに追加
            if (!worldMaterials.Contains(material))
            {
                worldMaterials.Add(material);
            }
        } 
        clippingPlane.enabled=false;       
    }

    // Update is called once per frame
    void Update()
    {
        
    }
	//他のオブジェクトが接触したときに呼ばれる
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Portal Entered");  
        //カメラの座標をゲートを原点にしたローカル座標に変換
        Vector3 localPos = transform.InverseTransformPoint(Camera.main.transform.position);
        //カメラがゲートの中心から横方向に0.5m以上離れている場合は無視
        if(Mathf.Abs(localPos.x)>0.5f) return; 
        //Alwaysを指定してWorldを常に表示
        SetStencilComparison(CompareFunction.Always);  
        //ゲートに接触したらクリッピング処理をオン
        clippingPlane.enabled = true;
        //裏か表を+-で表現
        enteringSide = Mathf.Sign(localPos.z);
        //条件に応じたクリッピング設定
        if((isARMode && enteringSide<0)||(!isARMode&&enteringSide>0)){
            clippingPlane.ClippingSide=ClippingPrimitive.Side.Outside;
        }
        else{
            clippingPlane.ClippingSide=ClippingPrimitive.Side.Inside;
        }
        //Clipping情報更新のフラグをOn
        clippingPlane.IsDirty=true;
    }
    //接触終了時に呼ばれる
    void OnTriggerExit(Collider other)
    {
        Debug.Log("Portal Exited");
        //カメラの座標をゲートを原点にしたローカル座標に変換
        Vector3 localPos = transform.InverseTransformPoint(Camera.main.transform.position);
        //裏か表を+-で表現
        float exitingSide = Mathf.Sign(localPos.z);
        if(isARMode){//現在ARモード：
            if(exitingSide!=enteringSide){//入った方向と逆から出たならVRモードに切り替え
                SetStencilComparison(CompareFunction.NotEqual);
                isARMode = false;
            }
            else
            {//入った方向と同じ方向から出たならARモードのまま
                SetStencilComparison(CompareFunction.Equal);
            }
        }
        else{//現在VRモード：
            if(exitingSide!=enteringSide){//入った方向と逆から出たならARモードに切り替え
                SetStencilComparison(CompareFunction.Equal);
                isARMode = true;
            }
            else
            {//入った方向と同じ方向から出たならVRモードのまま
                SetStencilComparison(CompareFunction.NotEqual);
            }
        }
        //ゲートから離れたらクリッピング処理をオフ
        clippingPlane.enabled = false;
    }
    //アプリ終了時にEditor内の表示をARモードに戻しておく
    void OnDestroy()
    {
        SetStencilComparison(CompareFunction.Equal);
    }
    //引数で受け取った設定でステンシルの比較条件を変更する
    void SetStencilComparison(CompareFunction mode){
        //移動先の3Dモデルのマテリアルを取得
        foreach(Material material in worldMaterials)
        {
            //ステンシルの比較条件を変更
            material.SetInt("_StencilComparison", (int)mode);
        }
    }
}
