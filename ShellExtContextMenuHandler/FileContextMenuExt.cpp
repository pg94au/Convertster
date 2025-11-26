#include "FileContextMenuExt.h"
#include <strsafe.h>
#include <Shlwapi.h>
#include "common.h"
#pragma comment(lib, "shlwapi.lib")


extern HINSTANCE g_hInst;
extern long g_cDllRef;


FileContextMenuExt::FileContextMenuExt()
	: m_cRef(1),
	m_pszMenuText(const_cast<PWSTR>(L_Menu_Text)),
	m_pszVerb(Verb_Name),
	m_pwszVerb(L_Verb_Name),
	m_pwszVerbCanonicalName(L_Verb_Canonical_Name),
	m_pwszVerbHelpText(L_Verb_Help_Text)
{
	InterlockedIncrement(&g_cDllRef);
}

FileContextMenuExt::~FileContextMenuExt()
{
	InterlockedDecrement(&g_cDllRef);
}


void FileContextMenuExt::OnVerbDisplayFileName(HWND hWnd)
{
	for (size_t i = 0; i < m_vSelectedFiles.size() && i < 2; i++)
	{
		wchar_t szMessage[300];
		if (SUCCEEDED(StringCchPrintf(szMessage, ARRAYSIZE(szMessage), L"The selected file is:\r\n\r\n%s", m_vSelectedFiles[i].c_str())))
		{
			MessageBox(hWnd, szMessage, L_Friendly_Menu_Name, MB_OK);
		}
	}
}


void FileContextMenuExt::OnConvertToJpg(HWND hWnd)
{
	// For now just display the first selected file to demonstrate the action.
	if (!m_vSelectedFiles.empty())
	{
		wchar_t szMessage[300];
		if (SUCCEEDED(StringCchPrintf(szMessage, ARRAYSIZE(szMessage), L"Convert to JPG:\r\n\r\n%s", m_vSelectedFiles[0].c_str())))
		{
			MessageBox(hWnd, szMessage, L_Friendly_Menu_Name, MB_OK);
		}
	}
}

void FileContextMenuExt::OnConvertToPng(HWND hWnd)
{
	// For now just display the first selected file to demonstrate the action.
	if (!m_vSelectedFiles.empty())
	{
		wchar_t szMessage[300];
		if (SUCCEEDED(StringCchPrintf(szMessage, ARRAYSIZE(szMessage), L"Convert to PNG:\r\n\r\n%s", m_vSelectedFiles[0].c_str())))
		{
			MessageBox(hWnd, szMessage, L_Friendly_Menu_Name, MB_OK);
		}
	}
}


#pragma region IUnknown

// Query to the interface the component supported.
IFACEMETHODIMP FileContextMenuExt::QueryInterface(REFIID riid, void** ppv)
{
	static const QITAB qit[] =
	{
	QITABENT(FileContextMenuExt, IContextMenu),
	QITABENT(FileContextMenuExt, IShellExtInit),
		{ nullptr },
	};
	return QISearch(this, qit, riid, ppv);
}

// Increase the reference count for an interface on an object.
IFACEMETHODIMP_(ULONG) FileContextMenuExt::AddRef()
{
	return InterlockedIncrement(&m_cRef);
}

// Decrease the reference count for an interface on an object.
IFACEMETHODIMP_(ULONG) FileContextMenuExt::Release()
{
	ULONG cRef = InterlockedDecrement(&m_cRef);
	if (0 == cRef)
	{
		delete this;
	}

	return cRef;
}

#pragma endregion



#pragma region IShellExtInit

// Initialize the context menu handler.
IFACEMETHODIMP FileContextMenuExt::Initialize(LPCITEMIDLIST pidlFolder, LPDATAOBJECT pDataObj, HKEY hKeyProgID)
{
	if (nullptr == pDataObj)
	{
		return E_INVALIDARG;
	}

	HRESULT hr = E_FAIL;

	FORMATETC fe = { CF_HDROP, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
	STGMEDIUM stm;

	// The pDataObj pointer contains the objects being acted upon. In this 
	// example, we get an HDROP handle for enumerating the selected files and 
	// folders.
	if (SUCCEEDED(pDataObj->GetData(&fe, &stm)))
	{
		// Get an HDROP handle.
		HDROP hDrop = static_cast<HDROP>(GlobalLock(stm.hGlobal));
		if (hDrop != nullptr)
		{
			// Determine how many files are involved in this operation. This 
			// code sample displays the custom context menu item when only 
			// one file is selected. 
			UINT nFiles = DragQueryFile(hDrop, 0xFFFFFFFF, nullptr, 0);
			for (size_t i = 0; i < nFiles; i++)
			{
				wchar_t szSelectedFile[MAX_PATH] = { 0 };
				// Get the path of the file.
				if (0 != DragQueryFile(hDrop, static_cast<UINT>(i), szSelectedFile, ARRAYSIZE(szSelectedFile)))
				{
					m_vSelectedFiles.emplace_back(szSelectedFile);
					hr = S_OK;
					continue;
				}
				hr = E_FAIL;
				break;
			}

			GlobalUnlock(stm.hGlobal);
		}

		ReleaseStgMedium(&stm);
	}

	if (S_OK == hr)
	{
		for (auto file = m_vSelectedFiles.cbegin(); file != m_vSelectedFiles.cend(); ++file)
		{
			const wchar_t* dot = wcsrchr(file->c_str(), L'.');
			if (dot && 0 != _wcsicmp(dot, L_Associated_Type))
			{
				hr = E_INVALIDARG;
				break;
			}
		}
	}

	// If any value other than S_OK is returned from the method, the context 
	// menu item is not displayed.
	return hr;
}

#pragma endregion


#pragma region IContextMenu

//
//   FUNCTION: FileContextMenuExt::QueryContextMenu
//
//   PURPOSE: The Shell calls IContextMenu::QueryContextMenu to allow the 
//            context menu handler to add its menu items to the menu. It 
//            passes in the HMENU handle in the hmenu parameter. The 
//            indexMenu parameter is set to the index to be used for the 
//            first menu item that is to be added.
//
IFACEMETHODIMP FileContextMenuExt::QueryContextMenu(HMENU hMenu, UINT indexMenu, UINT idCmdFirst, UINT idCmdLast, UINT uFlags)
{
	// If uFlags include CMF_DEFAULTONLY then we should not do anything.
	if (CMF_DEFAULTONLY & uFlags)
	{
		return MAKE_HRESULT(SEVERITY_SUCCESS, 0, static_cast<USHORT>(0));
	}

	// Use either InsertMenu or InsertMenuItem to add menu items.
	// Learn how to add sub-menu from:
	// http://www.codeproject.com/KB/shell/ctxextsubmenu.aspx

	// Parent menu item
	MENUITEMINFOW mii = { sizeof(mii) };
	mii.fMask = MIIM_SUBMENU | MIIM_STRING | MIIM_FTYPE | MIIM_ID | MIIM_STATE;
	mii.wID = idCmdFirst + IDM_DISPLAY;
	mii.fType = MFT_STRING;
	mii.dwTypeData = m_pszMenuText;
	mii.fState = MFS_ENABLED;

	// Create a popup submenu
	HMENU hSubMenu = CreatePopupMenu();
	if (!hSubMenu)
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

	// Add "To JPG" and "To PNG" to sub menu
	if (!AppendMenuW(hSubMenu, MF_STRING, idCmdFirst + IDM_CONVERT_JPG, L"To JPG"))
	{
		DestroyMenu(hSubMenu);
		return HRESULT_FROM_WIN32(GetLastError());
	}

	if (!AppendMenuW(hSubMenu, MF_STRING, idCmdFirst + IDM_CONVERT_PNG, L"To PNG"))
	{
		DestroyMenu(hSubMenu);
		return HRESULT_FROM_WIN32(GetLastError());
	}

	mii.hSubMenu = hSubMenu;

	if (!InsertMenuItem(hMenu, indexMenu, TRUE, &mii))
	{
		DestroyMenu(hSubMenu);
		return HRESULT_FROM_WIN32(GetLastError());
	}

	// Add a separator.
	MENUITEMINFOW sep = { sizeof(sep) };
	sep.fMask = MIIM_TYPE;
	sep.fType = MFT_SEPARATOR;
	if (!InsertMenuItem(hMenu, indexMenu + 1, TRUE, &sep))
	{
		DestroyMenu(hSubMenu);
		return HRESULT_FROM_WIN32(GetLastError());
	}

	// Return an HRESULT value with the severity set to SEVERITY_SUCCESS. 
	// Set the code value to the offset of the largest command identifier 
	// that was assigned, plus one (1).
	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, static_cast<USHORT>(IDM_CONVERT_PNG + 1));
}


//
//   FUNCTION: FileContextMenuExt::InvokeCommand
//
//   PURPOSE: This method is called when a user clicks a menu item to tell 
//            the handler to run the associated command. The lpcmi parameter 
//            points to a structure that contains the needed information.
//
IFACEMETHODIMP FileContextMenuExt::InvokeCommand(LPCMINVOKECOMMANDINFO pici)
{
	BOOL fUnicode = FALSE;

	// Determine which structure is being passed in, CMINVOKECOMMANDINFO or 
	// CMINVOKECOMMANDINFOEX based on the cbSize member of lpcmi. Although 
	// the lpcmi parameter is declared in Shlobj.h as a CMINVOKECOMMANDINFO 
	// structure, in practice it often points to a CMINVOKECOMMANDINFOEX 
	// structure. This struct is an extended version of CMINVOKECOMMANDINFO 
	// and has additional members that allow Unicode strings to be passed.
	if (pici->cbSize == sizeof(CMINVOKECOMMANDINFOEX))
	{
		if (pici->fMask & CMIC_MASK_UNICODE)
		{
			fUnicode = TRUE;
		}
	}

	// Determines whether the command is identified by its offset or verb.
	// There are two ways to identify commands:
	// 
	   //   1) The command's verb string 
	   //   2) The command's identifier offset
	// 
	// If the high-order word of lpcmi->lpVerb (for the ANSI case) or 
	// lpcmi->lpVerbW (for the Unicode case) is nonzero, lpVerb or lpVerbW 
	// holds a verb string. If the high-order word is zero, the command 
	// offset is in the low-order word of lpcmi->lpVerb.

	// For the ANSI case, if the high-order word is not zero, the command's 
	// verb string is in lpcmi->lpVerb. 
	if (!fUnicode && HIWORD(pici->lpVerb))
	{
		// Is the verb supported by this context menu extension?
		if (StrCmpIA(pici->lpVerb, m_pszVerb) == 0)
		{
			OnVerbDisplayFileName(pici->hwnd);
		}
		else
		{
			// If the verb is not recognized by the context menu handler, it 
			// must return E_FAIL to allow it to be passed on to the other 
			// context menu handlers that might implement that verb.
			return E_FAIL;
		}
	}

	// For the Unicode case, if the high-order word is not zero, the 
	// command's verb string is in lpcmi->lpVerbW. 
	else if (fUnicode && HIWORD(reinterpret_cast<CMINVOKECOMMANDINFOEX*>(pici)->lpVerbW))
	{
		// Is the verb supported by this context menu extension?
		if (StrCmpIW(reinterpret_cast<CMINVOKECOMMANDINFOEX*>(pici)->lpVerbW, m_pwszVerb) == 0)
		{
			OnVerbDisplayFileName(pici->hwnd);
		}
		else
		{
			// If the verb is not recognized by the context menu handler, it 
			// must return E_FAIL to allow it to be passed on to the other 
			// context menu handlers that might implement that verb.
			return E_FAIL;
		}
	}

	// If the command cannot be identified through the verb string, then 
	// check the identifier offset.
	else
	{
		// Is the command identifier offset supported by this context menu 
		// extension?
		if (LOWORD(pici->lpVerb) == IDM_DISPLAY)
		{
			OnVerbDisplayFileName(pici->hwnd);
		}
		else if (LOWORD(pici->lpVerb) == IDM_CONVERT_JPG)
		{
			OnConvertToJpg(pici->hwnd);
		}
		else if (LOWORD(pici->lpVerb) == IDM_CONVERT_PNG)
		{
			OnConvertToPng(pici->hwnd);
		}
		else
		{
			// If the verb is not recognized by the context menu handler, it 
			// must return E_FAIL to allow it to be passed on to the other 
			// context menu handlers that might implement that verb.
			return E_FAIL;
		}
	}

	return S_OK;
}


//
//   FUNCTION: CFileContextMenuExt::GetCommandString
//
//   PURPOSE: If a user highlights one of the items added by a context menu 
//            handler, the handler's IContextMenu::GetCommandString method is 
//            called to request a Help text string that will be displayed on 
//            the Windows Explorer status bar. This method can also be called 
//            to request the verb string that is assigned to a command. 
//            Either ANSI or Unicode verb strings can be requested. This 
//            example only implements support for the Unicode values of 
//            uFlags, because only those have been used in Windows Explorer 
//            since Windows 2000.
//
IFACEMETHODIMP FileContextMenuExt::GetCommandString(UINT_PTR idCommand,	UINT uFlags, UINT* pwReserved, LPSTR pszName, UINT cchMax)
{
	HRESULT hr = E_INVALIDARG;

	if (idCommand == IDM_DISPLAY)
	{
		switch (uFlags)
		{
		case GCS_HELPTEXTW:
			// Only useful for pre-Vista versions of Windows that have a 
			// Status bar.
			hr = StringCchCopy(reinterpret_cast<PWSTR>(pszName), cchMax, m_pwszVerbHelpText);
			break;

		case GCS_VERBW:
			// GCS_VERBW is an optional feature that enables a caller to 
			// discover the canonical name for the verb passed in through 
			// idCommand.
			hr = StringCchCopy(reinterpret_cast<PWSTR>(pszName), cchMax, m_pwszVerbCanonicalName);
			break;

		default:
			hr = S_OK;
		}
	}

	// If the command (idCommand) is not supported by this context menu 
	// extension handler, return E_INVALIDARG.

	return hr;
}

#pragma endregion
