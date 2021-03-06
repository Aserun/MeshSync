﻿#pragma once
#include "MeshSync/MeshSync.h"

#ifdef mscDebug
    #define mscTrace(...) ::mu::Print("MeshSync trace: " __VA_ARGS__)
#else
    #define mscTrace(...)
#endif

class MeshSyncClientMaya
{
public:
    enum class TargetScope
    {
        Unknown,
        Selection,
        All,
    };
    static MeshSyncClientMaya& getInstance();

    MeshSyncClientMaya(MObject obj);
    ~MeshSyncClientMaya();

    void setServerAddress(const char *v);
    void setServerPort(uint16_t v);
    void setAutoSync(bool v);
    void setSyncAnimations(bool v);
    void setAnimationSPS(int v);
    void setSyncBlendShapes(bool v);

    void update();
    void onSelectionChanged();
    void onSceneUpdated();

    void notifyUpdateTransform(MObject obj, bool force = false);
    void notifyUpdateCamera(MObject obj, bool force = false);
    void notifyUpdateLight(MObject obj, bool force = false);
    void notifyUpdateMesh(MObject obj, bool force = false);
    bool sendUpdatedObjects();
    bool sendScene(TargetScope scope = TargetScope::All);
    bool importScene();

private:
    bool isAsyncSendInProgress() const;
    void waitAsyncSend();
    void registerGlobalCallbacks();
    void registerNodeCallbacks(TargetScope scope = TargetScope::All);
    void removeGlobalCallbacks();
    void removeNodeCallbacks();
    int getMaterialID(MUuid uid);
    int getObjectID(MUuid uid);
    void extractAllMaterialData();
    bool extractTransformData(ms::Transform& dst, MObject src);
    bool extractCameraData(ms::Camera& dst, MObject src);
    bool extractLightData(ms::Light& dst, MObject src);
    bool extractMeshData(ms::Mesh& dst, MObject src);
    void kickAsyncSend();

private:
    using ExistRecords = std::map<std::string, bool>;

    MObject m_obj;
    MFnPlugin m_iplugin;
    bool m_auto_sync = false;
    bool m_export_meshes = true;
    bool m_export_skinning = true;
    bool m_export_cameras = true;
    bool m_export_lights = true;
    bool m_apply_tweak = true;
    bool m_sync_animations = true;
    bool m_sync_blend_shapes = true;
    int m_timeout_ms = 5000;
    int m_animation_samples_per_seconds = 10;
    float m_scale_factor = 1.0f;
    bool m_bake_skin = false;
    bool m_bake_cloth = false;

    std::vector<MCallbackId> m_cids_global;
    std::vector<MCallbackId> m_cids_node;
    std::vector<MUuid> m_material_id_table;
    std::vector<MUuid> m_object_id_table;
    std::vector<MObject> m_mtransforms;
    std::vector<MObject> m_mcameras;
    std::vector<MObject> m_mlights;
    std::vector<MObject> m_mmeshes;

    ms::ClientSettings              m_client_settings;
    std::vector<ms::TransformPtr>   m_client_transforms;
    std::vector<ms::CameraPtr>      m_client_cameras;
    std::vector<ms::LightPtr>       m_client_lights;
    std::vector<ms::MeshPtr>        m_client_meshes;
    std::vector<ms::MaterialPtr>    m_client_materials;
    std::vector<std::string>        m_deleted;
    ExistRecords        m_exist_record;
    std::mutex          m_mutex_extract_mesh;
    std::future<void>   m_future_send;
    bool                m_pending_send_scene = false;
    bool                m_scene_updated = false;
};
