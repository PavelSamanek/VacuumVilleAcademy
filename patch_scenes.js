const fs = require('fs');

const SCENES_DIR = 'C:/Claude/VacuumVilleAcademy/Assets/Scenes';
const MARKER = '--- !u!1660057539 &9223372036854775807';

function patchScene(sceneName, newYaml) {
    const path = `${SCENES_DIR}/${sceneName}`;
    let content = fs.readFileSync(path, 'utf8');
    content = content.replace(MARKER, newYaml.trimEnd() + '\n' + MARKER);
    fs.writeFileSync(path, content);
    console.log(`Patched: ${sceneName}`);
}

function makeSpawnPoint(goId, rtId, parentRtId, x, y, name) {
    return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parentRtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: ${x}, y: ${y}}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}`;
}

function makeTMPText(goId, rtId, crId, tmpId, parentRtId, x, y, w, h, text, name) {
    return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${tmpId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parentRtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: ${x}, y: ${y}}
  m_SizeDelta: {x: ${w}, y: ${h}}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
--- !u!114 &${tmpId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name:
  m_EditorClassIdentifier: TMPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_raycastTarget: 0
  m_text: '${text}'
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 0}
  m_sharedMaterial: {fileID: 0}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {r: 1, g: 1, b: 1, a: 1}
    topRight: {r: 1, g: 1, b: 1, a: 1}
    bottomLeft: {r: 1, g: 1, b: 1, a: 1}
    bottomRight: {r: 1, g: 1, b: 1, a: 1}
  m_fontColorGradientPreset: {fileID: 0}
  m_spriteAsset: {fileID: 0}
  m_tintAllSprites: 0
  m_StyleSheet: {fileID: 0}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: 48
  m_fontSizeBase: 48
  m_fontWeight: 400
  m_enableAutoSizing: 1
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: 1
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: 1
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 1
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isSelfOverlapping: 0
  m_isScrollRegion: 0
  m_vertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {x: 0, y: 0, z: 0, w: 0}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontReferences: 0
  m_baseMaterial: {fileID: 0}
  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}`;
}

function makeButton(goId, rtId, crId, imgId, btnId, lblGoId, lblRtId, lblCrId, lblTmpId, parentRtId, x, y, name) {
    return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  - component: {fileID: ${btnId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {fileID: ${lblRtId}}
  m_Father: {fileID: ${parentRtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0}
  m_AnchorMax: {x: 0.5, y: 0}
  m_AnchoredPosition: {x: ${x}, y: ${y}}
  m_SizeDelta: {x: 200, y: 80}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
--- !u!114 &${imgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: 1, g: 1, b: 1, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1
--- !u!114 &${btnId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 4e29b1a8efbd4b44bb3f3716e73f07ff, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Button
  m_Interactable: 1
  m_Transitions: 1
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_OnClick:
    m_PersistentCalls:
      m_Calls: []
--- !u!1 &${lblGoId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${lblRtId}}
  - component: {fileID: ${lblCrId}}
  - component: {fileID: ${lblTmpId}}
  m_Layer: 5
  m_Name: Label
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${lblRtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${lblGoId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${rtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0, y: 0}
  m_AnchorMax: {x: 1, y: 1}
  m_AnchoredPosition: {x: 0, y: 0}
  m_SizeDelta: {x: 0, y: 0}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${lblCrId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${lblGoId}}
  m_CullTransparentMesh: 1
--- !u!114 &${lblTmpId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${lblGoId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: f4688fdb7df04437aeb418b961361dc5, type: 3}
  m_Name:
  m_EditorClassIdentifier: TMPro::TMPro.TextMeshProUGUI
  m_Material: {fileID: 0}
  m_raycastTarget: 0
  m_text: '?'
  m_isRightToLeft: 0
  m_fontAsset: {fileID: 0}
  m_sharedMaterial: {fileID: 0}
  m_fontSharedMaterials: []
  m_fontMaterial: {fileID: 0}
  m_fontMaterials: []
  m_fontColor32:
    serializedVersion: 2
    rgba: 4294967295
  m_fontColor: {r: 1, g: 1, b: 1, a: 1}
  m_enableVertexGradient: 0
  m_colorMode: 3
  m_fontColorGradient:
    topLeft: {r: 1, g: 1, b: 1, a: 1}
    topRight: {r: 1, g: 1, b: 1, a: 1}
    bottomLeft: {r: 1, g: 1, b: 1, a: 1}
    bottomRight: {r: 1, g: 1, b: 1, a: 1}
  m_fontColorGradientPreset: {fileID: 0}
  m_spriteAsset: {fileID: 0}
  m_tintAllSprites: 0
  m_StyleSheet: {fileID: 0}
  m_TextStyleHashCode: -1183493901
  m_overrideHtmlColors: 0
  m_faceColor:
    serializedVersion: 2
    rgba: 4294967295
  m_fontSize: 48
  m_fontSizeBase: 48
  m_fontWeight: 400
  m_enableAutoSizing: 1
  m_fontSizeMin: 18
  m_fontSizeMax: 72
  m_fontStyle: 1
  m_HorizontalAlignment: 2
  m_VerticalAlignment: 512
  m_textAlignment: 65535
  m_characterSpacing: 0
  m_wordSpacing: 0
  m_lineSpacing: 0
  m_lineSpacingMax: 0
  m_paragraphSpacing: 0
  m_charWidthMaxAdj: 0
  m_enableWordWrapping: 1
  m_wordWrappingRatios: 0.4
  m_overflowMode: 0
  m_linkedTextComponent: {fileID: 0}
  parentLinkedComponent: {fileID: 0}
  m_enableKerning: 1
  m_enableExtraPadding: 0
  checkPaddingRequired: 0
  m_isRichText: 1
  m_parseCtrlCharacters: 1
  m_isOrthographic: 1
  m_isSelfOverlapping: 0
  m_isScrollRegion: 0
  m_vertexBufferAutoSizeReduction: 0
  m_useMaxVisibleDescender: 1
  m_pageToDisplay: 1
  m_margin: {x: 0, y: 0, z: 0, w: 0}
  m_isUsingLegacyAnimationComponent: 0
  m_isVolumetricText: 0
  m_hasFontReferences: 0
  m_baseMaterial: {fileID: 0}
  m_maskOffset: {x: 0, y: 0, z: 0, w: 0}`;
}

function makeSlider(goId, rtId, crId, sliderId, parentRtId, x, y, w, h, name) {
    return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${sliderId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parentRtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 1}
  m_AnchorMax: {x: 0.5, y: 1}
  m_AnchoredPosition: {x: ${x}, y: ${y}}
  m_SizeDelta: {x: ${w}, y: ${h}}
  m_Pivot: {x: 0.5, y: 1}
--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
--- !u!114 &${sliderId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 67db9e8f0e2ae9c40bc1e2b64352a6b4, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Slider
  m_Interactable: 0
  m_Transitions: 0
  m_Colors:
    m_NormalColor: {r: 1, g: 1, b: 1, a: 1}
    m_HighlightedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_PressedColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 1}
    m_SelectedColor: {r: 0.9607843, g: 0.9607843, b: 0.9607843, a: 1}
    m_DisabledColor: {r: 0.78431374, g: 0.78431374, b: 0.78431374, a: 0.5019608}
    m_ColorMultiplier: 1
    m_FadeDuration: 0.1
  m_SpriteState:
    m_HighlightedSprite: {fileID: 0}
    m_PressedSprite: {fileID: 0}
    m_SelectedSprite: {fileID: 0}
    m_DisabledSprite: {fileID: 0}
  m_AnimationTriggers:
    m_NormalTrigger: Normal
    m_HighlightedTrigger: Highlighted
    m_PressedTrigger: Pressed
    m_SelectedTrigger: Selected
    m_DisabledTrigger: Disabled
  m_Navigation:
    m_Mode: 3
    m_WrapAround: 0
    m_SelectOnUp: {fileID: 0}
    m_SelectOnDown: {fileID: 0}
    m_SelectOnLeft: {fileID: 0}
    m_SelectOnRight: {fileID: 0}
  m_FillRect: {fileID: 0}
  m_HandleRect: {fileID: 0}
  m_Direction: 0
  m_MinValue: 0
  m_MaxValue: 1
  m_WholeNumbers: 0
  m_Value: 1
  m_OnValueChanged:
    m_PersistentCalls:
      m_Calls: []`;
}

function makeImageGO(goId, rtId, crId, imgId, parentRtId, x, y, w, h, r, g, b, name) {
    return `--- !u!1 &${goId}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: ${rtId}}
  - component: {fileID: ${crId}}
  - component: {fileID: ${imgId}}
  m_Layer: 5
  m_Name: ${name}
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &${rtId}
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${parentRtId}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: ${x}, y: ${y}}
  m_SizeDelta: {x: ${w}, y: ${h}}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!222 &${crId}
CanvasRenderer:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_CullTransparentMesh: 1
--- !u!114 &${imgId}
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: ${goId}}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: fe87c0e1cc204ed48ad3b37840f39efc, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.Image
  m_Material: {fileID: 0}
  m_Color: {r: ${r}, g: ${g}, b: ${b}, a: 1}
  m_RaycastTarget: 1
  m_RaycastPadding: {x: 0, y: 0, z: 0, w: 0}
  m_Maskable: 1
  m_OnCullStateChanged:
    m_PersistentCalls:
      m_Calls: []
  m_Sprite: {fileID: 0}
  m_Type: 0
  m_PreserveAspect: 0
  m_FillCenter: 1
  m_FillMethod: 4
  m_FillAmount: 1
  m_FillClockwise: 1
  m_FillOrigin: 0
  m_UseSpriteMesh: 0
  m_PixelsPerUnitMultiplier: 1`;
}

// ===== BOXTOWERBUILDER =====
{
    const canvasRT = 1733273436;
    const yaml = [
        makeSpawnPoint(340000000, 340000010, canvasRT, 0, 700, 'SpawnLine'),
        makeSpawnPoint(340000020, 340000030, canvasRT, 0, -700, 'StackLine')
    ].join('\n');
    patchScene('Minigame_BoxTowerBuilder.unity', yaml);

    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_BoxTowerBuilder.unity`, 'utf8');
    content = content.replace('  boxPrefab: {fileID: 0}\n  spawnLine: {fileID: 0}\n  stackLine: {fileID: 0}',
        '  boxPrefab: {fileID: 100000, guid: d4e5f6789012345678901234dddddddd, type: 3}\n  spawnLine: {fileID: 340000010}\n  stackLine: {fileID: 340000030}');
    content = content.replace(
        '  - {fileID: 659212671}\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 0}',
        '  - {fileID: 659212671}\n  - {fileID: 340000010}\n  - {fileID: 340000030}\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 0}'
    );
    fs.writeFileSync(`${SCENES_DIR}/Minigame_BoxTowerBuilder.unity`, content);
    console.log('BoxTowerBuilder fields updated');
}

// ===== STREAMERUNTANGLESPRINT =====
{
    const canvasRT = 1733273436;
    const btn0 = makeButton(350000000, 350000010, 350000011, 350000012, 350000013,
                             350000020, 350000021, 350000022, 350000030, canvasRT, -200, -350, 'KnotAnswerBtn_0');
    const btn1 = makeButton(350000040, 350000050, 350000051, 350000052, 350000053,
                             350000060, 350000061, 350000062, 350000070, canvasRT, 200, -350, 'KnotAnswerBtn_1');
    const probText = makeTMPText(350000100, 350000110, 350000111, 350000120, canvasRT, 0, 200, 800, 120, '', 'KnotProblemText');
    const timerBar = makeSlider(350000200, 350000210, 350000211, 350000220, canvasRT, 0, -250, 800, 30, 'KnotTimerBar');

    const yaml = [btn0, btn1, probText, timerBar].join('\n');
    patchScene('Minigame_StreamerUntangleSprint.unity', yaml);

    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_StreamerUntangleSprint.unity`, 'utf8');
    content = content.replace(
        '  knotTimerBar: {fileID: 0}\n  knotTimerDuration: 5\n  knotProblemText: {fileID: 0}\n  knotAnswerButtons: []\n  knotAnswerLabels: []',
        '  knotTimerBar: {fileID: 350000220}\n  knotTimerDuration: 5\n  knotProblemText: {fileID: 350000120}\n  knotAnswerButtons:\n  - {fileID: 350000013}\n  - {fileID: 350000053}\n  knotAnswerLabels:\n  - {fileID: 350000030}\n  - {fileID: 350000070}'
    );
    content = content.replace(
        '  - {fileID: 659212671}\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 0}',
        '  - {fileID: 659212671}\n  - {fileID: 350000010}\n  - {fileID: 350000050}\n  - {fileID: 350000110}\n  - {fileID: 350000210}\n  m_Father: {fileID: 0}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0, y: 0}\n  m_AnchorMax: {x: 0, y: 0}'
    );
    fs.writeFileSync(`${SCENES_DIR}/Minigame_StreamerUntangleSprint.unity`, content);
    console.log('StreamerUntangleSprint fields updated');
}

// ===== FLOWERBEDFRENZ Y =====
{
    const canvasRT = 849951976;
    const btn0 = makeButton(360000000, 360000010, 360000011, 360000012, 360000013,
                             360000020, 360000021, 360000022, 360000030, canvasRT, -300, -500, 'AnswerBtn_0');
    const btn1 = makeButton(360000040, 360000050, 360000051, 360000052, 360000053,
                             360000060, 360000061, 360000062, 360000070, canvasRT, 0, -500, 'AnswerBtn_1');
    const btn2 = makeButton(360000080, 360000090, 360000091, 360000092, 360000093,
                             360000100, 360000101, 360000102, 360000110, canvasRT, 300, -500, 'AnswerBtn_2');
    const rowLabel = makeTMPText(360000200, 360000210, 360000211, 360000220, canvasRT, 0, 300, 800, 100, '', 'RowQuestionLabel');
    const gardenGrid = `--- !u!1 &360000300
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  serializedVersion: 6
  m_Component:
  - component: {fileID: 360000310}
  - component: {fileID: 360000311}
  m_Layer: 5
  m_Name: GardenGrid
  m_TagString: Untagged
  m_Icon: {fileID: 0}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!224 &360000310
RectTransform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 360000300}
  m_LocalRotation: {x: 0, y: 0, z: 0, w: 1}
  m_LocalPosition: {x: 0, y: 0, z: 0}
  m_LocalScale: {x: 1, y: 1, z: 1}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {fileID: ${canvasRT}}
  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}
  m_AnchorMin: {x: 0.5, y: 0.5}
  m_AnchorMax: {x: 0.5, y: 0.5}
  m_AnchoredPosition: {x: 0, y: 100}
  m_SizeDelta: {x: 700, y: 300}
  m_Pivot: {x: 0.5, y: 0.5}
--- !u!114 &360000311
MonoBehaviour:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 360000300}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: 8a8695521f0d02e499659fee002a07f0, type: 3}
  m_Name:
  m_EditorClassIdentifier: UnityEngine.UI::UnityEngine.UI.GridLayoutGroup
  m_Padding:
    m_Left: 5
    m_Right: 5
    m_Top: 5
    m_Bottom: 5
  m_ChildAlignment: 4
  m_Spacing: {x: 5, y: 5}
  m_StartCorner: 0
  m_StartAxis: 0
  m_CellSize: {x: 80, y: 80}
  m_Constraint: 1
  m_ConstraintCount: 5`;

    const yaml = [btn0, btn1, btn2, rowLabel, gardenGrid].join('\n');
    patchScene('Minigame_FlowerBedFrenzy.unity', yaml);

    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_FlowerBedFrenzy.unity`, 'utf8');
    content = content.replace(
        '  gardenGrid: {fileID: 0}\n  flowerCellPrefab: {fileID: 0}\n  gridColumns: 5\n  answerButtons: []\n  answerLabels: []\n  rowQuestionLabel: {fileID: 0}',
        '  gardenGrid: {fileID: 360000311}\n  flowerCellPrefab: {fileID: 100000, guid: e5f678901234567890123456eeeeeeee, type: 3}\n  gridColumns: 5\n  answerButtons:\n  - {fileID: 360000013}\n  - {fileID: 360000053}\n  - {fileID: 360000093}\n  answerLabels:\n  - {fileID: 360000030}\n  - {fileID: 360000070}\n  - {fileID: 360000110}\n  rowQuestionLabel: {fileID: 360000220}'
    );
    content = content.replace(
        '  - {fileID: 1242237637}\n  m_Father: {fileID: 0}',
        '  - {fileID: 1242237637}\n  - {fileID: 360000010}\n  - {fileID: 360000050}\n  - {fileID: 360000090}\n  - {fileID: 360000210}\n  - {fileID: 360000310}\n  m_Father: {fileID: 0}'
    );
    fs.writeFileSync(`${SCENES_DIR}/Minigame_FlowerBedFrenzy.unity`, content);
    console.log('FlowerBedFrenzy fields updated');
}

// ===== ATTICBINBLITZ =====
{
    const canvasRT = 849951976;
    const convSpawn = makeSpawnPoint(370000000, 370000010, canvasRT, -500, 200, 'ConveyorSpawn');
    const convEnd = makeSpawnPoint(370000020, 370000030, canvasRT, 500, 200, 'ConveyorEnd');
    const bin0Tgt = makeTMPText(370000100, 370000110, 370000111, 370000120, canvasRT, -300, -350, 150, 50, '?', 'Bin0TargetLabel');
    const bin0Cur = makeTMPText(370000130, 370000140, 370000141, 370000150, canvasRT, -300, -430, 150, 50, '0', 'Bin0CurrentLabel');
    const bin0 = makeImageGO(370000160, 370000170, 370000171, 370000172, canvasRT, -300, -400, 150, 150, 0.7, 0.7, 0.7, 'Bin_0');
    const bin1Tgt = makeTMPText(370000200, 370000210, 370000211, 370000220, canvasRT, 0, -350, 150, 50, '?', 'Bin1TargetLabel');
    const bin1Cur = makeTMPText(370000230, 370000240, 370000241, 370000250, canvasRT, 0, -430, 150, 50, '0', 'Bin1CurrentLabel');
    const bin1 = makeImageGO(370000260, 370000270, 370000271, 370000272, canvasRT, 0, -400, 150, 150, 0.7, 0.7, 0.7, 'Bin_1');
    const bin2Tgt = makeTMPText(370000300, 370000310, 370000311, 370000320, canvasRT, 300, -350, 150, 50, '?', 'Bin2TargetLabel');
    const bin2Cur = makeTMPText(370000330, 370000340, 370000341, 370000350, canvasRT, 300, -430, 150, 50, '0', 'Bin2CurrentLabel');
    const bin2 = makeImageGO(370000360, 370000370, 370000371, 370000372, canvasRT, 300, -400, 150, 150, 0.7, 0.7, 0.7, 'Bin_2');

    const yaml = [convSpawn, convEnd, bin0Tgt, bin0Cur, bin0, bin1Tgt, bin1Cur, bin1, bin2Tgt, bin2Cur, bin2].join('\n');
    patchScene('Minigame_AtticBinBlitz.unity', yaml);

    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_AtticBinBlitz.unity`, 'utf8');
    content = content.replace(
        '  conveyorSpawn: {fileID: 0}\n  conveyorEnd: {fileID: 0}\n  itemPrefab: {fileID: 0}\n  bins: []',
        '  conveyorSpawn: {fileID: 370000010}\n  conveyorEnd: {fileID: 370000030}\n  itemPrefab: {fileID: 100000, guid: f678901234567890123456789fffffff, type: 3}\n  bins:\n  - transform: {fileID: 370000170}\n    targetLabel: {fileID: 370000120}\n    currentLabel: {fileID: 370000150}\n    targetCount: 0\n    currentCount: 0\n  - transform: {fileID: 370000270}\n    targetLabel: {fileID: 370000220}\n    currentLabel: {fileID: 370000250}\n    targetCount: 0\n    currentCount: 0\n  - transform: {fileID: 370000370}\n    targetLabel: {fileID: 370000320}\n    currentLabel: {fileID: 370000350}\n    targetCount: 0\n    currentCount: 0'
    );
    content = content.replace(
        '  - {fileID: 1242237637}\n  m_Father: {fileID: 0}',
        '  - {fileID: 1242237637}\n  - {fileID: 370000010}\n  - {fileID: 370000030}\n  - {fileID: 370000110}\n  - {fileID: 370000140}\n  - {fileID: 370000170}\n  - {fileID: 370000210}\n  - {fileID: 370000240}\n  - {fileID: 370000270}\n  - {fileID: 370000310}\n  - {fileID: 370000340}\n  - {fileID: 370000370}\n  m_Father: {fileID: 0}'
    );
    fs.writeFileSync(`${SCENES_DIR}/Minigame_AtticBinBlitz.unity`, content);
    console.log('AtticBinBlitz fields updated');
}

// ===== SEQUENCESPRINKLER =====
// Need 8 sprinklerHeads: each has Button+Image+CR as button, TMP label child, Image indicator child
// SprinklerHead struct: button (Button component), label (TMP), indicator (Image), assignedNumber, activated
// Base IDs: 380000000+, 8 heads
// Head layout: 4x2 grid, x: -450,-150,150,450, y: 200,-200
{
    const canvasRT = 849951976;
    let yaml = '';
    const positions = [
        [-450, 200], [-150, 200], [150, 200], [450, 200],
        [-450, -200], [-150, -200], [150, -200], [450, -200]
    ];

    const headBtnIds = [];
    const headLblIds = [];
    const headIndIds = [];

    for (let i = 0; i < 8; i++) {
        const base = 380000000 + i * 100;
        const [x, y] = positions[i];

        // Main GO (button root): GO, RT, CR, Image, Button
        const goId = base;
        const rtId = base + 10;
        const crId = base + 11;
        const imgId = base + 12;
        const btnId = base + 13;
        // Label child
        const lblGoId = base + 20;
        const lblRtId = base + 21;
        const lblCrId = base + 22;
        const lblTmpId = base + 30;
        // Indicator child
        const indGoId = base + 40;
        const indRtId = base + 41;
        const indCrId = base + 42;
        const indImgId = base + 43;

        headBtnIds.push(btnId);
        headLblIds.push(lblTmpId);
        headIndIds.push(indImgId);

        yaml += makeButton(goId, rtId, crId, imgId, btnId, lblGoId, lblRtId, lblCrId, lblTmpId, canvasRT, x, y, `SprinklerHead_${i}`) + '\n';
        yaml += makeImageGO(indGoId, indRtId, indCrId, indImgId, rtId, 0, 0, 20, 20, 1, 1, 1, `Indicator_${i}`) + '\n';

        // Add indicator as child of button RT - need to fix the RT children list
        // The makeButton already creates RT with only lblRtId as child, we need to add indRtId too
    }

    patchScene('Minigame_SequenceSprinkler.unity', yaml);

    // Update sprinklerHeads array in MB
    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_SequenceSprinkler.unity`, 'utf8');

    // Build the sprinklerHeads array
    let headsYaml = '  sprinklerHeads:\n';
    for (let i = 0; i < 8; i++) {
        const base = 380000000 + i * 100;
        const btnId = base + 13;
        const lblTmpId = base + 30;
        const indImgId = base + 43;
        headsYaml += `  - button: {fileID: ${btnId}}\n    label: {fileID: ${lblTmpId}}\n    indicator: {fileID: ${indImgId}}\n    assignedNumber: 0\n    activated: 0\n`;
    }

    content = content.replace('  sprinklerHeads: []', headsYaml.trimEnd());

    // Add children to canvas - add all 8 button RTs + indicator RTs
    let newChildren = '';
    for (let i = 0; i < 8; i++) {
        const base = 380000000 + i * 100;
        newChildren += `  - {fileID: ${base + 10}}\n`;
    }
    // Also add indicator RTs as children of their button RTs - but indicators are nested
    // Actually indicators need to be children of the button RT
    // The makeButton creates RT with just lblRtId child, we need to also add indRtId
    for (let i = 0; i < 8; i++) {
        const base = 380000000 + i * 100;
        const rtId = base + 10;
        const lblRtId = base + 21;
        const indRtId = base + 41;
        content = content.replace(
            `  m_Children:\n  - {fileID: ${lblRtId}}\n  m_Father: {fileID: ${canvasRT}}`,
            `  m_Children:\n  - {fileID: ${lblRtId}}\n  - {fileID: ${indRtId}}\n  m_Father: {fileID: ${canvasRT}}`
        );
        // Fix indicator parent to be button RT not canvasRT
        content = content.replace(
            `  m_Father: {fileID: ${canvasRT}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 20, y: 20}`,
            `  m_Father: {fileID: ${rtId}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 20, y: 20}`
        );
    }

    content = content.replace(
        '  - {fileID: 668273828}\n  m_Father: {fileID: 0}',
        `  - {fileID: 668273828}\n${newChildren}  m_Father: {fileID: 0}`
    );

    fs.writeFileSync(`${SCENES_DIR}/Minigame_SequenceSprinkler.unity`, content);
    console.log('SequenceSprinkler fields updated');
}

// ===== GRANDHALLRESTORATION =====
{
    const canvasRT = 849951976;
    // Need: shapeTilePrefab, tileSpawnLine, mosaicRegions (array of MosaicRegion)
    // MosaicRegion: shapeKey (string), button (Button), fillImage (Image), totalSlots, filledSlots
    // Create spawn line + 4 mosaic regions
    const spawnLine = makeSpawnPoint(390000000, 390000010, canvasRT, 0, 800, 'TileSpawnLine');

    // 4 mosaic regions: triangle, square, circle, star
    const shapes = ['triangle', 'square', 'circle', 'star'];
    const shapePositions = [[-400, -500], [-200, -500], [200, -500], [400, -500]];
    let regionsYaml = '';
    const regionBtnIds = [];
    const regionImgIds = [];

    for (let i = 0; i < 4; i++) {
        const base = 390000100 + i * 100;
        const [x, y] = shapePositions[i];
        const lblGoId = base + 50;
        const lblRtId = base + 51;
        const lblCrId = base + 52;
        const lblTmpId = base + 53;

        regionsYaml += makeButton(base, base+10, base+11, base+12, base+13, lblGoId, lblRtId, lblCrId, lblTmpId, canvasRT, x, y, `Region_${shapes[i]}`) + '\n';
        // FillImage as separate image GO
        regionsYaml += makeImageGO(base+60, base+70, base+71, base+72, base+10, 0, 0, 160, 80, 0.2, 0.6, 1.0, `FillImage_${shapes[i]}`) + '\n';
        regionBtnIds.push(base + 13);
        regionImgIds.push(base + 72);
    }

    const yaml = [spawnLine, regionsYaml].join('\n');
    patchScene('Minigame_GrandHallRestoration.unity', yaml);

    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_GrandHallRestoration.unity`, 'utf8');

    // Build mosaicRegions
    let regionsStr = '  mosaicRegions:\n';
    for (let i = 0; i < 4; i++) {
        const base = 390000100 + i * 100;
        regionsStr += `  - shapeKey: ${shapes[i]}\n    button: {fileID: ${base+13}}\n    fillImage: {fileID: ${base+72}}\n    totalSlots: 5\n    filledSlots: 0\n`;
    }

    content = content.replace(
        '  mosaicRegions: []\n  shapeTilePrefab: {fileID: 0}\n  tileSpawnLine: {fileID: 0}',
        regionsStr.trimEnd() + '\n  shapeTilePrefab: {fileID: 100000, guid: 789012345678901234567890aaaabbbb, type: 3}\n  tileSpawnLine: {fileID: 390000010}'
    );

    // Add canvas children
    let newChildren = `  - {fileID: 390000010}\n`;
    for (let i = 0; i < 4; i++) {
        const base = 390000100 + i * 100;
        newChildren += `  - {fileID: ${base+10}}\n  - {fileID: ${base+70}}\n`;
    }
    content = content.replace(
        '  - {fileID: 1242237637}\n  m_Father: {fileID: 0}',
        `  - {fileID: 1242237637}\n${newChildren}  m_Father: {fileID: 0}`
    );

    // Fix fillImage parent to be region RT
    for (let i = 0; i < 4; i++) {
        const base = 390000100 + i * 100;
        const rtId = base + 10;
        content = content.replace(
            `  m_Father: {fileID: ${canvasRT}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 160, y: 80}`,
            `  m_Father: {fileID: ${rtId}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n  m_AnchoredPosition: {x: 0, y: 0}\n  m_SizeDelta: {x: 160, y: 80}`
        );
    }

    fs.writeFileSync(`${SCENES_DIR}/Minigame_GrandHallRestoration.unity`, content);
    console.log('GrandHallRestoration fields updated');
}

// ===== SCRAMBLERSH UTDOWN =====
// 5 PanelUI objects, each with: panelBackground(Image), problemLabel(TMP), answerButtons[3](Button), answerLabels[3](TMP), panelTimer(Slider), solvedOverlay(Image)
// Base: 400000000+, each panel takes 200 IDs
{
    const canvasRT = 849951976;
    let panelYaml = '';

    // 5 panels arranged vertically or in a row
    // Panel positions: x = -480,-240,0,240,480 at y=0
    const panelXPositions = [-480, -240, 0, 240, 480];
    const topicNames = ['Addition', 'Subtraction', 'Multiplication', 'Division', 'NumberOrdering'];

    const panelData = [];

    for (let p = 0; p < 5; p++) {
        const base = 400000000 + p * 200;
        const x = panelXPositions[p];

        // Panel background Image GO
        panelYaml += makeImageGO(base, base+10, base+11, base+12, canvasRT, x, 0, 180, 600, 0.2, 0.2, 0.3, `Panel_${topicNames[p]}`) + '\n';

        // Problem label TMP
        panelYaml += makeTMPText(base+20, base+30, base+31, base+32, base+10, 0, 200, 160, 80, '', `ProblemLabel_${p}`) + '\n';

        // 3 Answer buttons
        const btnIds = [];
        const lblIds = [];
        for (let b = 0; b < 3; b++) {
            const btnBase = base + 40 + b * 20;
            const lblBase = btnBase + 10;
            panelYaml += makeButton(btnBase, btnBase+1, btnBase+2, btnBase+3, btnBase+4,
                                     lblBase, lblBase+1, lblBase+2, lblBase+3,
                                     base+10, 0, 50 - b*80, `AnswerBtn_${p}_${b}`) + '\n';
            btnIds.push(btnBase + 4);
            lblIds.push(lblBase + 3);
        }

        // Panel timer slider
        panelYaml += makeSlider(base+100, base+110, base+111, base+112, base+10, 0, -200, 160, 20, `PanelTimer_${p}`) + '\n';

        // Solved overlay Image
        panelYaml += makeImageGO(base+120, base+130, base+131, base+132, base+10, 0, 0, 180, 600, 0.4, 0.9, 0.4, `SolvedOverlay_${p}`) + '\n';

        panelData.push({
            bgImgId: base + 12,
            problemLblId: base + 32,
            btnIds,
            lblIds,
            timerSliderId: base + 112,
            overlayImgId: base + 132,
            panelRtId: base + 10
        });
    }

    patchScene('Minigame_ScramblerShutdown.unity', panelYaml);

    let content = fs.readFileSync(`${SCENES_DIR}/Minigame_ScramblerShutdown.unity`, 'utf8');

    // Build panels array
    let panelsStr = '  panels:\n';
    const topicEnums = [0, 1, 2, 3, 4]; // MathTopic enum values
    for (let p = 0; p < 5; p++) {
        const pd = panelData[p];
        panelsStr += `  - topic: ${topicEnums[p]}\n`;
        panelsStr += `    panelBackground: {fileID: ${pd.bgImgId}}\n`;
        panelsStr += `    problemLabel: {fileID: ${pd.problemLblId}}\n`;
        panelsStr += `    answerButtons:\n`;
        for (const btnId of pd.btnIds) {
            panelsStr += `    - {fileID: ${btnId}}\n`;
        }
        panelsStr += `    answerLabels:\n`;
        for (const lblId of pd.lblIds) {
            panelsStr += `    - {fileID: ${lblId}}\n`;
        }
        panelsStr += `    panelTimer: {fileID: ${pd.timerSliderId}}\n`;
        panelsStr += `    solvedOverlay: {fileID: ${pd.overlayImgId}}\n`;
    }

    content = content.replace('  panels: []', panelsStr.trimEnd());

    // Add canvas children
    let newChildren = '';
    for (let p = 0; p < 5; p++) {
        const base = 400000000 + p * 200;
        newChildren += `  - {fileID: ${base+10}}\n`;
    }
    content = content.replace(
        '  - {fileID: 1242237637}\n  m_Father: {fileID: 0}',
        `  - {fileID: 1242237637}\n${newChildren}  m_Father: {fileID: 0}`
    );

    // Fix nested GOs: problem labels, timer, overlay need parent = panel RT not canvasRT
    // The makeTMPText and makeSlider and makeImageGO already set parentRtId = base+10 so they should be correct
    // But we need to fix the RT children of panel RT (base+10) to include all sub-objects
    // Actually the panel RTs as created will have m_Children: [] since we just made them with makeImageGO
    // We need to add the children to the panel RTs
    for (let p = 0; p < 5; p++) {
        const base = 400000000 + p * 200;
        const panelRtId = base + 10;
        const problemRtId = base + 30;
        const btn0RtId = base + 41;
        const btn1RtId = base + 61;
        const btn2RtId = base + 81;
        const timerRtId = base + 110;
        const overlayRtId = base + 130;

        // Fix panel RT to have children
        content = content.replace(
            `  m_Children: []\n  m_Father: {fileID: ${canvasRT}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n  m_AnchoredPosition: {x: ${panelXPositions[p]}, y: 0}\n  m_SizeDelta: {x: 180, y: 600}`,
            `  m_Children:\n  - {fileID: ${problemRtId}}\n  - {fileID: ${btn0RtId}}\n  - {fileID: ${btn1RtId}}\n  - {fileID: ${btn2RtId}}\n  - {fileID: ${timerRtId}}\n  - {fileID: ${overlayRtId}}\n  m_Father: {fileID: ${canvasRT}}\n  m_LocalEulerAnglesHint: {x: 0, y: 0, z: 0}\n  m_AnchorMin: {x: 0.5, y: 0.5}\n  m_AnchorMax: {x: 0.5, y: 0.5}\n  m_AnchoredPosition: {x: ${panelXPositions[p]}, y: 0}\n  m_SizeDelta: {x: 180, y: 600}`
        );
    }

    fs.writeFileSync(`${SCENES_DIR}/Minigame_ScramblerShutdown.unity`, content);
    console.log('ScramblerShutdown fields updated');
}

console.log('All scene patches complete!');
