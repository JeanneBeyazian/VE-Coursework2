﻿using System.Collections;
using System.Collections.Generic;
using Ubiq.Messaging;
using Ubiq.XR;
using Ubiq.Samples;
using UnityEngine;

public class Teleporting : MonoBehaviour, INetworkObject, INetworkComponent
{   

    private NetworkContext context;
    private Rigidbody body;
    private static int COOLDOWN = 1;
    
    public static float ACTIVE_LIFETIME = 70f; 
    
    public static float INACTIVE_LIFETIME = 70f; 
	
	public static int MAXIMUM_ACTIVE_PORTAL_PAIRS = 1;
	// Integer for the maximum number of portals we can have active at once.
	
	public static int MAXIMUM_INACTIVE_PORTALS_OF_ONE_TYPE = 1;
	
	public static List<GameObject> inactiveEntryPortals	= new List<GameObject>();
	// List holding all the inactive entry portals
	
	public static List<GameObject> inactiveExitPortals = new List<GameObject>();
	// List holding all the inactive exit portals
	
	public static List<GameObject> activeEntryPortals = new List<GameObject>();
	// List holding all the active entry portals
	
	public static List<GameObject> activeExitPortals = new List<GameObject>();
	// List holding all the active exit portals
    
    public GameObject linkedPortal; 
    // The portal this is linked to
	
	public Plane plane; 
	// The plane on show
    public Camera camera; 
    public Material activeMaterial;
    public Material inactiveMaterial;
// Material to hold RenderTexture

    public GameObject TextName;
    // Contains the text on the portal
    
    private TextMesh textNameMesh;
    // Controls the text on the portal

    public void UpdateText() {
        textNameMesh = TextName.GetComponent<TextMesh> ();
        if (textNameMesh) {
            textNameMesh = TextName.GetComponent<TextMesh> ();
        }
        if (textNameMesh) {
            textNameMesh.color = (linkedPortal) ? Color.green : Color.red;
            textNameMesh.text = this.tag;
        }

        transform.GetChild(3).GetComponent<MeshRenderer>().materials = new Material []
            {(linkedPortal) ? activeMaterial : inactiveMaterial};
    }

    
    public void Start(){
        context = NetworkScene.Register(this);
        textNameMesh = TextName.GetComponent<TextMesh> ();
        UpdateText();
        context.SendJson(new Message(this.gameObject.transform.position));

        Material material = new Material(Shader.Find("Specular"));
        camera.targetTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        material.mainTexture = camera.targetTexture;
        transform.GetChild(1).GetComponent<MeshRenderer>().materials = new Material [] {material};
    }
    public static void addPortal(GameObject portal)
	{
        if (portal.tag == "EXIT") {
            ProcessNewPortal(portal, inactiveExitPortals, inactiveEntryPortals);
        }
        else if (portal.tag == "ENTRY") {
            ProcessNewPortal(portal, inactiveEntryPortals, inactiveExitPortals);
        }
    }
    
    private static void ProcessNewPortal(GameObject portal,
                                         List<GameObject> addToInactive,
                                         List<GameObject> otherInactive) {
                                             
        Debug.Log("Creating portal with tag " + portal.tag);
        
        

        if (addToInactive.Count >= MAXIMUM_INACTIVE_PORTALS_OF_ONE_TYPE) {

            if (addToInactive.Count != 0) {

                Destroy(addToInactive[0]);
            }
            // Delete the oldest one
        }
        
        addToInactive.Add(portal);
    
        if (addToInactive.Count <= otherInactive.Count) {
            // If there are more or equal inactive portals (of the other kind)
            
            CheckDestroyOldestPortalPair();
        
            int idx = addToInactive.Count - 1;
            
            Teleporting entryComponent = inactiveEntryPortals[idx].GetComponent<Teleporting>();
            // We can link the two.
            Teleporting exitComponent = inactiveExitPortals[idx].GetComponent<Teleporting>();
            
            entryComponent.LinkCameraPortal(inactiveExitPortals[idx]);
            exitComponent.linkedPortal = inactiveEntryPortals[idx];
            
            entryComponent.UpdateText();
            exitComponent.UpdateText();
                
            activeExitPortals.Add(inactiveExitPortals[idx]);
            activeEntryPortals.Add(inactiveEntryPortals[idx]);
            // As they are linked an active, they need to be added to the active portals list
                
            inactiveEntryPortals.RemoveAt(idx);
            inactiveExitPortals.RemoveAt(idx);
            // And removed from their original list
        } 
        else portal.GetComponent<Teleporting>().UpdateText();
    }
    
    private static void CheckDestroyOldestPortalPair() {

        if (activeEntryPortals.Count == 0) return;

        if (activeEntryPortals.Count >= MAXIMUM_ACTIVE_PORTAL_PAIRS) {

            GameObject oldLink = activeExitPortals[0];
            Destroy(activeEntryPortals[0]); 
            // Destroy ENTRY first as it renders other's graphics.
            Destroy(oldLink);
        }
    }
    
    void OnDestroy() { // Linked Portals are unlinked if this one is destroyed.
        if (linkedPortal) {
            // If the destroyed portal had a linked portal,
            activeEntryPortals.Remove(this.gameObject);
            activeExitPortals.Remove(linkedPortal);
            // We remove them from being active.

            Teleporting TPLinkedPortal = linkedPortal.GetComponent<Teleporting>();
            
            TPLinkedPortal.linkedPortal = null;
            TPLinkedPortal.UpdateText();
            
            if (this.tag == "ENTRY") {
                if (inactiveExitPortals.Count < MAXIMUM_INACTIVE_PORTALS_OF_ONE_TYPE) {
                        addPortal(linkedPortal);
                    // If it was an entry portal, its exit portal is thrown back into the inactive pool.
                } else {
                    if (linkedPortal) Destroy(linkedPortal);
                    // If there's no space, the linkedPortal is given an index for destruction.
                }
               
            }
            else if (this.tag == "EXIT") {
                if (inactiveEntryPortals.Count < MAXIMUM_INACTIVE_PORTALS_OF_ONE_TYPE) {
                    Material material = new Material(Shader.Find("Specular"));

                    TPLinkedPortal.camera.targetTexture = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);

                    material.mainTexture = TPLinkedPortal.camera.targetTexture;

                    MeshRenderer r = linkedPortal.transform.GetChild(1).GetComponent<MeshRenderer>();
                    if (r) r.materials = new Material[] {material};

                    // If it was an exit portal, then its entry portal's graphics are updated
                    addPortal(linkedPortal);
                    // And if there's space for it, it is returned to the pool
                } else {
                    if (linkedPortal) Destroy(linkedPortal);
                    // Otherwise it would delete other set ups and so it is destroyed instead
                }
            }
        } else {
            if (this.tag == "ENTRY") inactiveEntryPortals.Remove(this.gameObject);
            else if (this.tag == "EXIT") inactiveExitPortals.Remove(this.gameObject);
            // Unlinked portals are processed just by removing their null index from the list.
        }
    }

 
    private void Awake()
    {
        body = GetComponent<Rigidbody>();
    }
    
    private void LinkCameraPortal(GameObject otherPortal) {
        this.linkedPortal = otherPortal;
    }



    void OnTriggerEnter(Collider other)
    {   

        Debug.Log("Enter Portal" + this.tag + other.tag);
        
        UpdateText();
        
        if (other.tag == "DESTROY") {
            Destroy(this.gameObject, 0.1f);
            Destroy(other.gameObject, 0.1f);
            return;
        }
        if (!linkedPortal || this.tag != "ENTRY") return;
        if (other.tag == "Player"){ 
            CollideScript cs = other.gameObject.GetComponent<CollideScript>();
            if (!cs.canTeleport) return;
            cs.canTeleport = false;
            StartCoroutine(BeginPlayerTeleportCooldown(cs));

        }
          
        Vector3 targetPos = linkedPortal.transform.position;
        targetPos.y -=1f;
            
        Debug.Log("Attempted to teleport");
        other.gameObject.transform.position = targetPos;
        if (other.tag == "Player"){
            //Quaternion newQuaternion = new Quaternion();
            other.gameObject.transform.rotation = Quaternion.Euler(0, linkedPortal.transform.eulerAngles.y , 0);
        } else {
            other.gameObject.transform.rotation = linkedPortal.transform.rotation;
        }
        


    }
    
    IEnumerator BeginPlayerTeleportCooldown(CollideScript player){
        yield return new WaitForSeconds(COOLDOWN);
        player.canTeleport = true;
    }
    
    public NetworkId Id { get; set; }
    public struct Message
    {
       public Vector3 position;
       public Message(Vector3 p) {
           position = p;
       }
       
    }
    
    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<Message>();
     
    }

}
