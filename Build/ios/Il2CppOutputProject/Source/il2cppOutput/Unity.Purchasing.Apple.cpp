#include "pch-cpp.hpp"






struct DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771;
struct AsyncCallback_t7FEF460CBDCFB9C5FA2EF776984778B9A4145F4C;
struct DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E;
struct IAsyncResult_t7B9B5A0ECB35DCEC31B8A8122C37D687369253B5;
struct MethodInfo_t;
struct StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570;
struct String_t;
struct UnityPurchasingCallback_t3C1333A45134D9A999AB29AEEBF05883A9A707F3;
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915;
struct iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0;

IL2CPP_EXTERN_C RuntimeClass* Marshal_tD976A56A90263C3CE2B780D4B1CADADE2E70B4A7_il2cpp_TypeInfo_var;
IL2CPP_EXTERN_C String_t* _stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709;
struct Delegate_t_marshaled_com;
struct Delegate_t_marshaled_pinvoke;


IL2CPP_EXTERN_C_BEGIN
IL2CPP_EXTERN_C_END

#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
struct U3CModuleU3E_t61AF874F149C9609D7FF09D5A7130BC8BBDA6650 
{
};
struct String_t  : public RuntimeObject
{
	int32_t ____stringLength;
	Il2CppChar ____firstChar;
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F  : public RuntimeObject
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_pinvoke
{
};
struct ValueType_t6D9B272BD21782F0A9A14F2E41F85A50E97A986F_marshaled_com
{
};
struct iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0  : public RuntimeObject
{
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22 
{
	bool ___m_value;
};
struct Int32_t680FF22E76F6EFAD4375103CBBFFA0421349384C 
{
	int32_t ___m_value;
};
struct IntPtr_t 
{
	void* ___m_value;
};
struct Void_t4861ACF8F4594C3437BB48B6E56783494B843915 
{
	union
	{
		struct
		{
		};
		uint8_t Void_t4861ACF8F4594C3437BB48B6E56783494B843915__padding[1];
	};
};
struct Delegate_t  : public RuntimeObject
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	RuntimeObject* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	bool ___method_is_virtual;
};
struct Delegate_t_marshaled_pinvoke
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	Il2CppIUnknown* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	int32_t ___method_is_virtual;
};
struct Delegate_t_marshaled_com
{
	intptr_t ___method_ptr;
	intptr_t ___invoke_impl;
	Il2CppIUnknown* ___m_target;
	intptr_t ___method;
	intptr_t ___delegate_trampoline;
	intptr_t ___extra_arg;
	intptr_t ___method_code;
	intptr_t ___interp_method;
	intptr_t ___interp_invoke_impl;
	MethodInfo_t* ___method_info;
	MethodInfo_t* ___original_method_info;
	DelegateData_t9B286B493293CD2D23A5B2B5EF0E5B1324C2B77E* ___data;
	int32_t ___method_is_virtual;
};
struct MulticastDelegate_t  : public Delegate_t
{
	DelegateU5BU5D_tC5AB7E8F745616680F337909D3A8E6C722CDF771* ___delegates;
};
struct MulticastDelegate_t_marshaled_pinvoke : public Delegate_t_marshaled_pinvoke
{
	Delegate_t_marshaled_pinvoke** ___delegates;
};
struct MulticastDelegate_t_marshaled_com : public Delegate_t_marshaled_com
{
	Delegate_t_marshaled_com** ___delegates;
};
struct StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570  : public MulticastDelegate_t
{
};
struct UnityPurchasingCallback_t3C1333A45134D9A999AB29AEEBF05883A9A707F3  : public MulticastDelegate_t
{
};
struct String_t_StaticFields
{
	String_t* ___Empty;
};
struct Boolean_t09A6377A54BE2F9E6985A8149F19234FD7DDFE22_StaticFields
{
	String_t* ___TrueString;
	String_t* ___FalseString;
};
struct IntPtr_t_StaticFields
{
	intptr_t ___Zero;
};
#ifdef __clang__
#pragma clang diagnostic pop
#endif



IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_SetNativeCallback_m48C165929A4DFD0ABC5015941D3F46A68F46474D (UnityPurchasingCallback_t3C1333A45134D9A999AB29AEEBF05883A9A707F3* ___0_callbackDelegate, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR intptr_t iOSStoreBindings_unityPurchasing_FetchAppReceipt_mD2F9C5650B0A670C22FB4CD71A52D5AAE46C2ACF (const RuntimeMethod* method) ;
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool IntPtr_op_Inequality_m90EFC9C4CAD9A33E309F2DDF98EE4E1DD253637B_inline (intptr_t ___0_value1, intptr_t ___1_value2, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* Marshal_PtrToStringAuto_m163B3E46325675C58A42EB0C5C36B950DD9D1275 (intptr_t ___0_ptr, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_DeallocateMemory_mEBD5AACED8199802A7628078CD3396F89CC480E9 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, intptr_t ___0_pointer, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_DeallocateMemory_m038C009D4E37DF441F6E99D723C125A08DD3B168 (intptr_t ___0_pointer, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool iOSStoreBindings_unityPurchasing_CanMakePayments_mE673EACFA6FB9457CDDFF1536E883CCF164777BC (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_AddTransactionObserver_mF6FD8433EC4FAB82F25CBD213B03E32462B668D8 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchProducts_m394B980FD20D9985D4747CE1C0847793A28CF4B7 (String_t* ___0_json, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchPurchases_mAFFB0F84632511BB45B80DF162D37B8ABFD00253 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_Purchase_mC7E21E6F80754BB0A9169BAA577B8307A6800CD1 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productJson, String_t* ___1_optionsJson, StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570* ___2_callback, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478 (String_t* ___0_value, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FinishTransaction_mAC15FECCFE5690F2A2564C384DA071509930D7E1 (String_t* ___0_transactionId, bool ___1_logFinishTransaction, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool iOSStoreBindings_unityPurchasing_checkEntitlement_m4EFA623776641B3C17F35077CFDEB87638594B46 (String_t* ___0_productId, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_RestoreTransactions_m60FC0081733E7854F3439028D79C8AC3CF5B1BAF (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchStorePromotionOrder_m9744B2F56521360CB2953A834C985F9EA3B8C4C9 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_UpdateStorePromotionOrder_m0A8E46E7369153C2949C3BC0D3E01E143F7617D0 (String_t* ___0_json, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchStorePromotionVisibility_mA2321D47DE402E732410060A5CBE0F14466623CC (String_t* ___0_productId, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_UpdateStorePromotionVisibility_m26953D212D7E97BCE04686869D2FF3A1F55B559F (String_t* ___0_productId, String_t* ___1_visibility, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_InterceptPromotionalPurchases_mEFD2433F7E5D3F29B6C5BDF94FC1CE33F105CDF1 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_ContinuePromotionalPurchases_m2C8490DFD8E8FDE4295095CF3DDA27FBC16A6600 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_PresentCodeRedemptionSheet_m739E24429031D74599D6FE709878355EA7B7772A (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_PurchaseProduct_m8886EC379E68DA6DD5CD6A1F9C3416FFCBFC1858 (String_t* ___0_productJson, String_t* ___1_optionsJson, StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570* ___2_storefrontCallbackDelegate, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_RefreshAppReceipt_m06EF637EEE6B826AF51011D6F89E675AD09F7009 (const RuntimeMethod* method) ;
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2 (RuntimeObject* __this, const RuntimeMethod* method) ;
IL2CPP_EXTERN_C void STDCALL unityPurchasing_SetNativeCallback(Il2CppMethodPointer);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_AddTransactionObserver();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_FetchProducts(char*);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_PurchaseProduct(char*, char*, Il2CppMethodPointer);
IL2CPP_EXTERN_C intptr_t STDCALL unityPurchasing_FetchAppReceipt();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_DeallocateMemory(intptr_t);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_FetchPurchases();
IL2CPP_EXTERN_C char* STDCALL unityPurchasing_FetchTransactionForProductId(char*);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_FinishTransaction(char*, int32_t);
IL2CPP_EXTERN_C int32_t STDCALL unityPurchasing_checkEntitlement(char*);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_RestoreTransactions();
IL2CPP_EXTERN_C int32_t STDCALL unityPurchasing_CanMakePayments();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_FetchStorePromotionOrder();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_UpdateStorePromotionOrder(char*);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_FetchStorePromotionVisibility(char*);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_UpdateStorePromotionVisibility(char*, char*);
IL2CPP_EXTERN_C void STDCALL unityPurchasing_InterceptPromotionalPurchases();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_ContinuePromotionalPurchases();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_PresentCodeRedemptionSheet();
IL2CPP_EXTERN_C void STDCALL unityPurchasing_RefreshAppReceipt();
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
#ifdef __clang__
#pragma clang diagnostic pop
#endif
#ifdef __clang__
#pragma clang diagnostic push
#pragma clang diagnostic ignored "-Winvalid-offsetof"
#pragma clang diagnostic ignored "-Wunused-variable"
#endif
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_SetNativeCallback_m48C165929A4DFD0ABC5015941D3F46A68F46474D (UnityPurchasingCallback_t3C1333A45134D9A999AB29AEEBF05883A9A707F3* ___0_callbackDelegate, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (Il2CppMethodPointer);

	Il2CppMethodPointer ____0_callbackDelegate_marshaled = NULL;
	____0_callbackDelegate_marshaled = il2cpp_codegen_marshal_delegate(reinterpret_cast<MulticastDelegate_t*>(___0_callbackDelegate));

	reinterpret_cast<PInvokeFunc>(unityPurchasing_SetNativeCallback)(____0_callbackDelegate_marshaled);

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_AddTransactionObserver_mF6FD8433EC4FAB82F25CBD213B03E32462B668D8 (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_AddTransactionObserver)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchProducts_m394B980FD20D9985D4747CE1C0847793A28CF4B7 (String_t* ___0_json, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (char*);

	char* ____0_json_marshaled = NULL;
	____0_json_marshaled = il2cpp_codegen_marshal_string(___0_json);

	reinterpret_cast<PInvokeFunc>(unityPurchasing_FetchProducts)(____0_json_marshaled);

	il2cpp_codegen_marshal_free(____0_json_marshaled);
	____0_json_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_PurchaseProduct_m8886EC379E68DA6DD5CD6A1F9C3416FFCBFC1858 (String_t* ___0_productJson, String_t* ___1_optionsJson, StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570* ___2_storefrontCallbackDelegate, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (char*, char*, Il2CppMethodPointer);

	char* ____0_productJson_marshaled = NULL;
	____0_productJson_marshaled = il2cpp_codegen_marshal_string(___0_productJson);

	char* ____1_optionsJson_marshaled = NULL;
	____1_optionsJson_marshaled = il2cpp_codegen_marshal_string(___1_optionsJson);

	Il2CppMethodPointer ____2_storefrontCallbackDelegate_marshaled = NULL;
	____2_storefrontCallbackDelegate_marshaled = il2cpp_codegen_marshal_delegate(reinterpret_cast<MulticastDelegate_t*>(___2_storefrontCallbackDelegate));

	reinterpret_cast<PInvokeFunc>(unityPurchasing_PurchaseProduct)(____0_productJson_marshaled, ____1_optionsJson_marshaled, ____2_storefrontCallbackDelegate_marshaled);

	il2cpp_codegen_marshal_free(____0_productJson_marshaled);
	____0_productJson_marshaled = NULL;

	il2cpp_codegen_marshal_free(____1_optionsJson_marshaled);
	____1_optionsJson_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR intptr_t iOSStoreBindings_unityPurchasing_FetchAppReceipt_mD2F9C5650B0A670C22FB4CD71A52D5AAE46C2ACF (const RuntimeMethod* method) 
{
	typedef intptr_t (STDCALL *PInvokeFunc) ();

	intptr_t returnValue = reinterpret_cast<PInvokeFunc>(unityPurchasing_FetchAppReceipt)();

	return returnValue;
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_DeallocateMemory_m038C009D4E37DF441F6E99D723C125A08DD3B168 (intptr_t ___0_pointer, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (intptr_t);

	reinterpret_cast<PInvokeFunc>(unityPurchasing_DeallocateMemory)(___0_pointer);

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchPurchases_mAFFB0F84632511BB45B80DF162D37B8ABFD00253 (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_FetchPurchases)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* iOSStoreBindings_unityPurchasing_FetchTransactionForProductId_mEC70476780D546F60C02C2280CBF7870844163CF (String_t* ___0_productId, const RuntimeMethod* method) 
{
	typedef char* (STDCALL *PInvokeFunc) (char*);

	char* ____0_productId_marshaled = NULL;
	____0_productId_marshaled = il2cpp_codegen_marshal_string(___0_productId);

	char* returnValue = reinterpret_cast<PInvokeFunc>(unityPurchasing_FetchTransactionForProductId)(____0_productId_marshaled);

	String_t* _returnValue_unmarshaled = NULL;
	_returnValue_unmarshaled = il2cpp_codegen_marshal_string_result(returnValue);

	il2cpp_codegen_marshal_free(returnValue);
	returnValue = NULL;

	il2cpp_codegen_marshal_free(____0_productId_marshaled);
	____0_productId_marshaled = NULL;

	return _returnValue_unmarshaled;
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FinishTransaction_mAC15FECCFE5690F2A2564C384DA071509930D7E1 (String_t* ___0_transactionId, bool ___1_logFinishTransaction, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (char*, int32_t);

	char* ____0_transactionId_marshaled = NULL;
	____0_transactionId_marshaled = il2cpp_codegen_marshal_string(___0_transactionId);

	reinterpret_cast<PInvokeFunc>(unityPurchasing_FinishTransaction)(____0_transactionId_marshaled, static_cast<int32_t>(___1_logFinishTransaction));

	il2cpp_codegen_marshal_free(____0_transactionId_marshaled);
	____0_transactionId_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool iOSStoreBindings_unityPurchasing_checkEntitlement_m4EFA623776641B3C17F35077CFDEB87638594B46 (String_t* ___0_productId, const RuntimeMethod* method) 
{
	typedef int32_t (STDCALL *PInvokeFunc) (char*);

	char* ____0_productId_marshaled = NULL;
	____0_productId_marshaled = il2cpp_codegen_marshal_string(___0_productId);

	int32_t returnValue = reinterpret_cast<PInvokeFunc>(unityPurchasing_checkEntitlement)(____0_productId_marshaled);

	il2cpp_codegen_marshal_free(____0_productId_marshaled);
	____0_productId_marshaled = NULL;

	return static_cast<bool>(returnValue);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_RestoreTransactions_m60FC0081733E7854F3439028D79C8AC3CF5B1BAF (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_RestoreTransactions)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool iOSStoreBindings_unityPurchasing_CanMakePayments_mE673EACFA6FB9457CDDFF1536E883CCF164777BC (const RuntimeMethod* method) 
{
	typedef int32_t (STDCALL *PInvokeFunc) ();

	int32_t returnValue = reinterpret_cast<PInvokeFunc>(unityPurchasing_CanMakePayments)();

	return static_cast<bool>(returnValue);
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchStorePromotionOrder_m9744B2F56521360CB2953A834C985F9EA3B8C4C9 (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_FetchStorePromotionOrder)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_UpdateStorePromotionOrder_m0A8E46E7369153C2949C3BC0D3E01E143F7617D0 (String_t* ___0_json, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (char*);

	char* ____0_json_marshaled = NULL;
	____0_json_marshaled = il2cpp_codegen_marshal_string(___0_json);

	reinterpret_cast<PInvokeFunc>(unityPurchasing_UpdateStorePromotionOrder)(____0_json_marshaled);

	il2cpp_codegen_marshal_free(____0_json_marshaled);
	____0_json_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_FetchStorePromotionVisibility_mA2321D47DE402E732410060A5CBE0F14466623CC (String_t* ___0_productId, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (char*);

	char* ____0_productId_marshaled = NULL;
	____0_productId_marshaled = il2cpp_codegen_marshal_string(___0_productId);

	reinterpret_cast<PInvokeFunc>(unityPurchasing_FetchStorePromotionVisibility)(____0_productId_marshaled);

	il2cpp_codegen_marshal_free(____0_productId_marshaled);
	____0_productId_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_UpdateStorePromotionVisibility_m26953D212D7E97BCE04686869D2FF3A1F55B559F (String_t* ___0_productId, String_t* ___1_visibility, const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) (char*, char*);

	char* ____0_productId_marshaled = NULL;
	____0_productId_marshaled = il2cpp_codegen_marshal_string(___0_productId);

	char* ____1_visibility_marshaled = NULL;
	____1_visibility_marshaled = il2cpp_codegen_marshal_string(___1_visibility);

	reinterpret_cast<PInvokeFunc>(unityPurchasing_UpdateStorePromotionVisibility)(____0_productId_marshaled, ____1_visibility_marshaled);

	il2cpp_codegen_marshal_free(____0_productId_marshaled);
	____0_productId_marshaled = NULL;

	il2cpp_codegen_marshal_free(____1_visibility_marshaled);
	____1_visibility_marshaled = NULL;

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_InterceptPromotionalPurchases_mEFD2433F7E5D3F29B6C5BDF94FC1CE33F105CDF1 (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_InterceptPromotionalPurchases)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_ContinuePromotionalPurchases_m2C8490DFD8E8FDE4295095CF3DDA27FBC16A6600 (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_ContinuePromotionalPurchases)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_PresentCodeRedemptionSheet_m739E24429031D74599D6FE709878355EA7B7772A (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_PresentCodeRedemptionSheet)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_unityPurchasing_RefreshAppReceipt_m06EF637EEE6B826AF51011D6F89E675AD09F7009 (const RuntimeMethod* method) 
{
	typedef void (STDCALL *PInvokeFunc) ();

	reinterpret_cast<PInvokeFunc>(unityPurchasing_RefreshAppReceipt)();

}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_SetUnityPurchasingCallback_m219BD679AD861A63ED28739140610DB5DA5463D6 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, UnityPurchasingCallback_t3C1333A45134D9A999AB29AEEBF05883A9A707F3* ___0_AsyncCallback, const RuntimeMethod* method) 
{
	{
		UnityPurchasingCallback_t3C1333A45134D9A999AB29AEEBF05883A9A707F3* L_0 = ___0_AsyncCallback;
		iOSStoreBindings_unityPurchasing_SetNativeCallback_m48C165929A4DFD0ABC5015941D3F46A68F46474D(L_0, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR String_t* iOSStoreBindings_AppReceipt_mF5EA98F8BD47B15E76CF73B147730899D7ED8167 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	static bool s_Il2CppMethodInitialized;
	if (!s_Il2CppMethodInitialized)
	{
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&Marshal_tD976A56A90263C3CE2B780D4B1CADADE2E70B4A7_il2cpp_TypeInfo_var);
		il2cpp_codegen_initialize_runtime_metadata((uintptr_t*)&_stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709);
		s_Il2CppMethodInitialized = true;
	}
	intptr_t V_0;
	memset((&V_0), 0, sizeof(V_0));
	String_t* V_1 = NULL;
	{
		intptr_t L_0;
		L_0 = iOSStoreBindings_unityPurchasing_FetchAppReceipt_mD2F9C5650B0A670C22FB4CD71A52D5AAE46C2ACF(NULL);
		V_0 = L_0;
		V_1 = _stringLiteralDA39A3EE5E6B4B0D3255BFEF95601890AFD80709;
		intptr_t L_1 = V_0;
		bool L_2;
		L_2 = IntPtr_op_Inequality_m90EFC9C4CAD9A33E309F2DDF98EE4E1DD253637B_inline(L_1, 0, NULL);
		if (!L_2)
		{
			goto IL_0027;
		}
	}
	{
		intptr_t L_3 = V_0;
		il2cpp_codegen_runtime_class_init_inline(Marshal_tD976A56A90263C3CE2B780D4B1CADADE2E70B4A7_il2cpp_TypeInfo_var);
		String_t* L_4;
		L_4 = Marshal_PtrToStringAuto_m163B3E46325675C58A42EB0C5C36B950DD9D1275(L_3, NULL);
		V_1 = L_4;
		intptr_t L_5 = V_0;
		iOSStoreBindings_DeallocateMemory_mEBD5AACED8199802A7628078CD3396F89CC480E9(__this, L_5, NULL);
	}

IL_0027:
	{
		String_t* L_6 = V_1;
		return L_6;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_DeallocateMemory_mEBD5AACED8199802A7628078CD3396F89CC480E9 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, intptr_t ___0_pointer, const RuntimeMethod* method) 
{
	{
		intptr_t L_0 = ___0_pointer;
		iOSStoreBindings_unityPurchasing_DeallocateMemory_m038C009D4E37DF441F6E99D723C125A08DD3B168(L_0, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool iOSStoreBindings_get_canMakePayments_m28A7429A5777856D788F64097F63BAD019E00C92 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		bool L_0;
		L_0 = iOSStoreBindings_unityPurchasing_CanMakePayments_mE673EACFA6FB9457CDDFF1536E883CCF164777BC(NULL);
		return L_0;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_Connect_m33ACEC93479F2203C0D20A74BFE37568A9050868 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_AddTransactionObserver_mDE4C2E69B7C8FB368B0120240D957E89869FFE6A (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_AddTransactionObserver_mF6FD8433EC4FAB82F25CBD213B03E32462B668D8(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_FetchProducts_m5ECD8737AF6F6849004DD88099D6FBD3132605AF (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_json, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_json;
		iOSStoreBindings_unityPurchasing_FetchProducts_m394B980FD20D9985D4747CE1C0847793A28CF4B7(L_0, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_FetchExistingPurchases_m8163D347D521F7B2E5F0BF0912233A69D5E275EE (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_FetchPurchases_mAFFB0F84632511BB45B80DF162D37B8ABFD00253(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_Purchase_mDD137547381A32459D42B5E8274F1F1F27986826 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productJSON, String_t* ___1_developerPayload, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_productJSON;
		String_t* L_1 = ___1_developerPayload;
		iOSStoreBindings_Purchase_mC7E21E6F80754BB0A9169BAA577B8307A6800CD1(__this, L_0, L_1, (StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570*)NULL, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_FinishTransaction_mDD77BBC44F32A0E5E508DEA91519F15B8F91C3F6 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productDescription, String_t* ___1_transactionId, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___1_transactionId;
		String_t* L_1 = ___0_productDescription;
		bool L_2;
		L_2 = String_IsNullOrEmpty_mEA9E3FB005AC28FE02E69FCF95A7B8456192B478(L_1, NULL);
		iOSStoreBindings_unityPurchasing_FinishTransaction_mAC15FECCFE5690F2A2564C384DA071509930D7E1(L_0, (bool)((((int32_t)L_2) == ((int32_t)0))? 1 : 0), NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR bool iOSStoreBindings_CheckEntitlement_m20D68F01725E5A4857126EBDD29F93CE0543A64D (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productId, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_productId;
		bool L_1;
		L_1 = iOSStoreBindings_unityPurchasing_checkEntitlement_m4EFA623776641B3C17F35077CFDEB87638594B46(L_0, NULL);
		return L_1;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_RestoreTransactions_mE4FBAAB6141F2DD5411506DF435828A22947A0AF (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_RestoreTransactions_m60FC0081733E7854F3439028D79C8AC3CF5B1BAF(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_FetchStorePromotionOrder_m4BEBB7E966E4119AFB47A363DE4FD7B4D040AFD2 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_FetchStorePromotionOrder_m9744B2F56521360CB2953A834C985F9EA3B8C4C9(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_SetStorePromotionOrder_mD3F860F21F8035BE068CFAB3AD48DE72102998D4 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_json, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_json;
		iOSStoreBindings_unityPurchasing_UpdateStorePromotionOrder_m0A8E46E7369153C2949C3BC0D3E01E143F7617D0(L_0, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_FetchStorePromotionVisibility_mCB0C2EC9DDE3D77AC01FA69DB3E2C300F9C48698 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productId, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_productId;
		iOSStoreBindings_unityPurchasing_FetchStorePromotionVisibility_mA2321D47DE402E732410060A5CBE0F14466623CC(L_0, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_SetStorePromotionVisibility_m62BF7C1907BC81157CAD75A5E26B1C043DA752D1 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productId, String_t* ___1_visibility, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_productId;
		String_t* L_1 = ___1_visibility;
		iOSStoreBindings_unityPurchasing_UpdateStorePromotionVisibility_m26953D212D7E97BCE04686869D2FF3A1F55B559F(L_0, L_1, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_InterceptPromotionalPurchases_mEBF2EA86F9E9A85F1AD4D78ADABC830A3A32110E (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_InterceptPromotionalPurchases_mEFD2433F7E5D3F29B6C5BDF94FC1CE33F105CDF1(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_ContinuePromotionalPurchases_m520F9C789F5A5780007614F110690229CF763E14 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_ContinuePromotionalPurchases_m2C8490DFD8E8FDE4295095CF3DDA27FBC16A6600(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_PresentCodeRedemptionSheet_mA36018A743EBDBFF3E91A2806E7A9B5A907C46D2 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_PresentCodeRedemptionSheet_m739E24429031D74599D6FE709878355EA7B7772A(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_Purchase_mC7E21E6F80754BB0A9169BAA577B8307A6800CD1 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, String_t* ___0_productJson, String_t* ___1_optionsJson, StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570* ___2_callback, const RuntimeMethod* method) 
{
	{
		String_t* L_0 = ___0_productJson;
		String_t* L_1 = ___1_optionsJson;
		StorefrontChangeCallback_tFF0E50758B09B379FFAD47874880E4CEC6AFB570* L_2 = ___2_callback;
		iOSStoreBindings_unityPurchasing_PurchaseProduct_m8886EC379E68DA6DD5CD6A1F9C3416FFCBFC1858(L_0, L_1, L_2, NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings_RefreshAppReceipt_mB71B723604356E85F2A7FE5A27D4ECA3723BD453 (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		iOSStoreBindings_unityPurchasing_RefreshAppReceipt_m06EF637EEE6B826AF51011D6F89E675AD09F7009(NULL);
		return;
	}
}
IL2CPP_EXTERN_C IL2CPP_METHOD_ATTR void iOSStoreBindings__ctor_mE32A73607E2E7F2341870734F6EA96375BB760DB (iOSStoreBindings_t6D0B0FA1099D0078AA43F5214A8DFAE1663323B0* __this, const RuntimeMethod* method) 
{
	{
		Object__ctor_mE837C6B9FA8C6D5D109F4B2EC885D79919AC0EA2(__this, NULL);
		return;
	}
}
#ifdef __clang__
#pragma clang diagnostic pop
#endif
IL2CPP_MANAGED_FORCE_INLINE IL2CPP_METHOD_ATTR bool IntPtr_op_Inequality_m90EFC9C4CAD9A33E309F2DDF98EE4E1DD253637B_inline (intptr_t ___0_value1, intptr_t ___1_value2, const RuntimeMethod* method) 
{
	{
		intptr_t L_0 = ___0_value1;
		intptr_t L_1 = ___1_value2;
		return (bool)((((int32_t)((((intptr_t)L_0) == ((intptr_t)L_1))? 1 : 0)) == ((int32_t)0))? 1 : 0);
	}
}
