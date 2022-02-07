using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using Firebase;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Threading.Tasks;

public class TraileRenderer100 : MonoBehaviour
{
    public GameObject penPoint;
    public GameObject cube;
    public Text numposstored;
    public Text trailrendererstate;
    public Text info;
    public ARSessionOrigin arSessionOrigin;
    
    private LineRenderer lineRenderer;
    private ARPlaneManager arPlaneManager;
    private ARRaycastManager arRaycastManager;
    private ARAnchorManager arAnchorManager;
    private bool spawned=false;
    private List<ARRaycastHit> arRaycastHits=new List<ARRaycastHit>();
    private Vector2 touchPosition;
    private GameObject spawnedObject;
    private GameObject trailHolder;
    private ARAnchor localanchor;
    private List<List<Vector3>> trailInfo = new List<List<Vector3>> ();
    private List<List<Vector3>> trailinfoquery=new List<List<Vector3>>();
    private List<Vector3> trailStart=new List<Vector3>();
    private List<Vector3> temp=new List<Vector3>();
    private List<GameObject> objectList=new List<GameObject>();
    private int j=0;
    private int drawn=0;
    private int positionc=0;
    private bool drawing=false;
    FirebaseFirestore db;

    [FirestoreData]
    public struct fireupload{
        [FirestoreProperty]
        public float[] xcoordinate{get;set;}
        [FirestoreProperty]
        public float[] ycoordinate{get;set;}
        [FirestoreProperty]
        public float[] zcoordinate{get;set;}
        
    }
    
    void Awake(){
        arPlaneManager=arSessionOrigin.GetComponent<ARPlaneManager>();
        arRaycastManager=arSessionOrigin.GetComponent<ARRaycastManager>();
        arAnchorManager=arSessionOrigin.GetComponent<ARAnchorManager>();      
        db=Firebase.Firestore.FirebaseFirestore.DefaultInstance;
    }
    void Start()
    {
        
    }
    void Update()
    {
        if(!drawing)
        {
        if(Input.touchCount>0){
        Touch touch=Input.GetTouch(0);
        if(touch.phase==TouchPhase.Stationary || touch.phase==TouchPhase.Moved ){
            touchPosition=Input.GetTouch(0).position;
                if(arRaycastManager.Raycast(touchPosition,arRaycastHits,TrackableType.PlaneWithinPolygon)){
                    Pose hitPose=arRaycastHits[0].pose;
                    if(!spawned){
                        spawnedObject=Instantiate(penPoint,hitPose.position,hitPose.rotation);
                        trailHolder=Instantiate(cube,hitPose.position,hitPose.rotation);
                        objectList.Add(trailHolder);                       
                        lineRenderer=trailHolder.AddComponent<LineRenderer>();
                        lineRenderer.enabled=false;
                        lineRenderer.startWidth=0.007f;
                        localanchor=spawnedObject.AddComponent<ARAnchor>();
                        spawned=true;               
                        trailStart.Add(trailHolder.transform.position);
                    }
                    else if(spawned){
                        arPlaneManager.enabled=false;
                        //spawnedObject.SetActive(true);
                        //spawnedObject.transform.position=hitPose.position;
                        trailrendererstate.text="going to store";
                        //trailPos.Add(hitPose.position);
                        
                        temp.Add(hitPose.position);
                        positionc+=1;
                        trailrendererstate.text="postionc: "+positionc+"\n"+drawing;
                        StartCoroutine(smoothTransition(hitPose));
                    }
                }     
        }
        else if(touch.phase==TouchPhase.Ended)  {
            touchPosition=Input.GetTouch(0).position;
            if(arRaycastManager.Raycast(touchPosition,arRaycastHits,TrackableType.PlaneWithinPolygon)){ //else it counts touches outside too if it taps
                trailInfo.Add(temp);
                //temp.Clear();
                j+=1;
                lineRenderer.positionCount = positionc;     //VERY IMPORTANT: PositionCount should not be extraa, if you don't assign then some weird floating line renderer will exist to equalize
                positionc=0;
                drawing=true;
                info.text=""+trailInfo.Count;
                StartCoroutine(createtrail());}
            }

        }}
    }
      /* 
    MOST IMPORTANT THING: BELOW PASS POSE INSTEAD OF VECTOR2 HITPOSE.POSITION IT WILL NOT WORK. POSE GETS COMMUNICATED PROPERLY BUT VECTOR2 DOES NOT
    */
    IEnumerator smoothTransition(Pose hitPose){
            yield return new WaitForSeconds(0.05f);
            spawnedObject.transform.position=Vector3.MoveTowards(spawnedObject.transform.position,hitPose.position,0.6f);
            yield return null;
    }
    public void DestroyTrail(){
        trailStart.Clear();
        trailInfo.Clear();
        trailinfoquery.Clear();
        for(int i=0;i<objectList.Count;i++){
        Destroy(objectList[i]);
        }
        objectList.Clear();
        spawned=false;
        drawing=false;
        drawn=0;
        j=0;
        info.text=trailInfo.Count+":"+trailStart.Count;
    }
    public void callfire(){
        firestore();
    }
    async void firestore(){
        Query querres=db.Collection("coll");
        trailrendererstate.text="trying to get snapshot";
        QuerySnapshot querysnap=await querres.GetSnapshotAsync();
        int c=querysnap.Count;
        //Subcollection for each letter,
        List<float> xcoords;
        List<float> ycoords;
        List<float> zcoords;

        int tempcount=0;
        for(int i=0;i<trailInfo.Count;i++){
            xcoords=new List<float>();
            ycoords=new List<float>();
            zcoords=new List<float>();
            
            for(int q=0;q<trailInfo[i].Count;q++){
                tempcount+=1;
                trailrendererstate.text=i+":"+tempcount+" :"+trailInfo[i].Count;
                //trailrendererstate.text=trailInfo.Count+":"+trailInfo[i].Count;
                //info.text="trying first one "+i+":"+q;
                //info.text=q+":"+trailInfo[i][q].x;
                xcoords.Add(trailInfo[i][q].x);
                info.text="xcoords Count: "+xcoords.Count;
                //info.text=q+":"+xcoords[q];
                ycoords.Add(trailInfo[i][q].y);
                zcoords.Add(trailInfo[i][q].z);
            }
            var goingtodocref=new fireupload{
                xcoordinate=xcoords.ToArray(),
                ycoordinate=ycoords.ToArray(),
                zcoordinate=zcoords.ToArray()
            } ;
           int d=i+c;
           string docname="doc"+d;
           info.text="trying to set async";
           await db.Collection("coll").Document(docname).SetAsync(goingtodocref);
           info.text="set doc async";


        }
        
    }
    public void fireclearcall(){
        clearfirestore();
    }
    async void clearfirestore(){
        Query querres=db.Collection("coll");
        trailrendererstate.text="trying to get snapshot";
        QuerySnapshot querysnap=await querres.GetSnapshotAsync();
        foreach(DocumentSnapshot docsnap in querysnap.Documents){
            string id=docsnap.Id;
            await db.Collection("coll").Document(id).DeleteAsync();
        }
    }


    public void querycall(){
        querytest();
    }
    async void querytest(){
        Query querres=db.Collection("coll");
        trailrendererstate.text="trying to get snapshot";
        QuerySnapshot querysnap=await querres.GetSnapshotAsync();
        trailrendererstate.text="got snapshot";
        List<float[]> xquery=new List<float[]>();
        List<float[]> yquery=new List<float[]>();
        List<float[]> zquery=new List<float[]>();
        
        trailrendererstate.text="trying to get in xquery";
        foreach(DocumentSnapshot docsnap in querysnap.Documents){
            xquery.Add(docsnap.GetValue<float[]>("xcoordinate",ServerTimestampBehavior.None));
            yquery.Add(docsnap.GetValue<float[]>("ycoordinate",ServerTimestampBehavior.None));
            zquery.Add(docsnap.GetValue<float[]>("zcoordinate",ServerTimestampBehavior.None));
        }
        trailrendererstate.text="got in xquery";
        info.text="this:"+xquery.Count;
        trailrendererstate.text="trying to create trailinfo:";
        for(int i=0;i<xquery.Count;i++){
            List<Vector3> tempo=new List<Vector3>();
            for(int q=0;q<xquery[i].Length;q++){
                trailrendererstate.text="trying to create vector:";
                Vector3 newvec=new Vector3(xquery[i][q],yquery[i][q],zquery[i][q]);
                trailrendererstate.text="trying to add vector:";
                tempo.Add(newvec);
                trailrendererstate.text="vector added:";
            }
            trailrendererstate.text="trying to add to trailinfo:";
            trailinfoquery.Add(tempo);
            trailrendererstate.text="added to trailinfo:"+i;
        }
        StartCoroutine(createquerytrail(trailinfoquery));
        
    }
    IEnumerator createquerytrail(List<List<Vector3>> trailinfoquery){
            for(int i=0;i<trailinfoquery.Count;i++){
            GameObject querytrailHolder=Instantiate(cube,trailinfoquery[i][0],Quaternion.identity);
            objectList.Add(querytrailHolder);
            LineRenderer querylineRenderer=querytrailHolder.AddComponent<LineRenderer>();
            querylineRenderer.startWidth=0.007f;
            querylineRenderer.positionCount=trailinfoquery[i].Count;
            for(int q=0;q<trailinfoquery[i].Count;q++){
                querylineRenderer.SetPosition(q,trailinfoquery[i][q]);
                yield return new WaitForSeconds(0.06f);
            }
        }
        StopCoroutine(createquerytrail(trailinfoquery));
    }

    IEnumerator createtrail(){
        lineRenderer.enabled=true;  
        //lineRenderer.useWorldSpace=false; cant do this, line renderer doesnt work    
        for(int q=drawn;q<j;q++){    
        for(int i=0;i<trailInfo[q].Count;i++){
            spawnedObject.transform.position=trailInfo[q][i];
            lineRenderer.SetPosition(i,trailInfo[q][i]);
            //info.text=trailInfo.Count+":"+trailInfo[q].Count;
            yield return new WaitForSeconds(0.06f);}
            drawn+=1;  }
        Destroy(spawnedObject);
        spawned=false;
        drawing=false;
        
        //info.text=trailInfo.Count+":beforetemp:"+trailInfo[1].Count;
        temp=new List<Vector3>();
        StopCoroutine(createtrail());     
        trailrendererstate.text="Count:"+trailInfo.Count;   
        //numposstored.text=trailInfo.Count+":aftertemp   :"+trailInfo[1].Count;
        //spawnedObject.SetActive(false);
    }
}
