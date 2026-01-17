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


void FileContextMenuExt::OnConvertToJpg(HWND hWnd)
{
	if (!RunConverterCommand(hWnd, L"JPG"))
	{
		// If the conversion command failed, show a message box.
		MessageBox(hWnd, L"Failed to convert to JPG!", L_Friendly_Menu_Name, MB_OK | MB_ICONERROR);
	}
}

void FileContextMenuExt::OnConvertToPng(HWND hWnd)
{
	if (!RunConverterCommand(hWnd, L"PNG"))
	{
		// If the conversion command failed, show a message box.
		MessageBox(hWnd, L"Failed to convert to PNG!", L_Friendly_Menu_Name, MB_OK | MB_ICONERROR);
	}
}

bool FileContextMenuExt::RunConverterCommand(HWND hWnd, PCWSTR targetFormat)
{
	// Build command-line arguments: program path first, then the format,
	// then each filename quoted.
	//const wchar_t exePath[] = L"C:\\SHARED\\ImageConverter.exe";
	//const wchar_t exePath[] = L"C:\\Program Files\\Blinkenlights Image Converter\\ImageConverter.exe";
	std::wstring exePath;
	HKEY hKey = nullptr;

	LONG result = RegOpenKeyExW(
		HKEY_LOCAL_MACHINE,
		L"Software\\Blinkenlights Image Converter",
		0,
		KEY_READ,
		&hKey);

	if (result != ERROR_SUCCESS)
	{
		MessageBoxW(hWnd, L"Unable to open registry key for Blinkenlights Image Converter.", L_Friendly_Menu_Name, MB_OK | MB_ICONERROR);
		return false;
	}

	DWORD type = 0;
	DWORD size = 0;

	if (RegQueryValueExW(
		hKey,
		L"ExecutablePath",
		nullptr,
		&type,
		nullptr,
		&size) != ERROR_SUCCESS || type != REG_SZ)
	{
		RegCloseKey(hKey);
		return false;
	}

	exePath.resize(size / sizeof(wchar_t), L'\0');

	if (RegQueryValueExW(
		hKey,
		L"ExecutablePath",
		nullptr,
		nullptr,
		reinterpret_cast<LPBYTE>(&exePath[0]),
		&size) != ERROR_SUCCESS)
	{
		RegCloseKey(hKey);
		return false;
	}

	// Remove trailing null added by registry
	exePath.resize(wcslen(exePath.c_str()));

	RegCloseKey(hKey);

	// Verify executable exists.
	if (!PathFileExistsW(exePath.c_str()))
	{
		MessageBoxW(hWnd, L"ImageConverter.exe not found.", L_Friendly_Menu_Name, MB_OK | MB_ICONERROR);
		return false;
	}

	std::wstring cmdLineStr;
	cmdLineStr.append(L"\"");
	cmdLineStr.append(exePath);
	cmdLineStr.append(L"\" ");

	// Add format as first real argument.
	cmdLineStr.append(targetFormat);

	for (const auto& f : m_vSelectedFiles)
	{
		cmdLineStr.push_back(L' ');
		cmdLineStr.push_back(L'"');
		cmdLineStr.append(f);
		cmdLineStr.push_back(L'"');
	}

	// CreateProcess expects a mutable buffer for the command-line parameter.
	std::vector<wchar_t> cmdLine;
	//cmdLine.reserve(args.size() + 1);
	cmdLine.assign(cmdLineStr.begin(), cmdLineStr.end());
	cmdLine.push_back(L'\0');

	STARTUPINFOW si = {};
	si.cb = sizeof(si);
	PROCESS_INFORMATION pi = {};

	// Pass the executable path as lpApplicationName and args as lpCommandLine.
	BOOL created = CreateProcessW(
		nullptr,                   // lpApplicationName
		cmdLine.data(),            // lpCommandLine (mutable)
		nullptr,                   // lpProcessAttributes
		nullptr,                   // lpThreadAttributes
		FALSE,                     // bInheritHandles
		0,                         // dwCreationFlags
		nullptr,                   // lpEnvironment
		nullptr,                   // lpCurrentDirectory
		&si,                       // lpStartupInfo
		&pi                        // lpProcessInformation
	);

	if (!created)
	{
		DWORD err = GetLastError();
		wchar_t errMsg[256];
		StringCchPrintfW(errMsg, ARRAYSIZE(errMsg), L"CreateProcess failed (0x%08X).", static_cast<unsigned>(err));
		MessageBoxW(hWnd, errMsg, L_Friendly_Menu_Name, MB_OK | MB_ICONERROR);
		return false;
	}

	// We don't need to wait here; close handles and continue.
	CloseHandle(pi.hThread);
	CloseHandle(pi.hProcess);
	return true;
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
			// Allowed extensions (case-insensitive).
			const wchar_t* allowedExts[] = { L".bmp", L".png", L".tif", L".tiff", L".webp" };

			if (dot)
			{
				bool matched = false;
				for (const wchar_t* ext : allowedExts)
				{
					if (0 == _wcsicmp(dot, ext))
					{
						matched = true;
						break;
					}
				}

				if (!matched)
				{
					hr = E_INVALIDARG;
					break;
				}
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
	mii.fMask = MIIM_SUBMENU | MIIM_STRING | MIIM_FTYPE | MIIM_STATE;
	mii.fType = MFT_STRING;
	mii.dwTypeData = m_pszMenuText;
	mii.fState = MFS_ENABLED;

	// Create a popup submenu
	HMENU hSubMenu = CreatePopupMenu();
	if (!hSubMenu)
	{
		return HRESULT_FROM_WIN32(GetLastError());
	}

	// Determine if any selected file already has a .png extension (case-insensitive).
	bool anyHasPng = false;
	for (const auto& f : m_vSelectedFiles)
	{
		const wchar_t* dot = wcsrchr(f.c_str(), L'.');
		if (dot && 0 == _wcsicmp(dot, L".png"))
		{
			anyHasPng = true;
			break;
		}
	}

    // Add "To JPG" to sub menu (always available for supported input types)
    if (!AppendMenuW(hSubMenu, MF_STRING, idCmdFirst + IDM_CONVERT_JPG, L"To JPG"))
    {
        DestroyMenu(hSubMenu);
        return HRESULT_FROM_WIN32(GetLastError());
    }

    // Add "To PNG" only if none of the selected files are already PNGs
    bool pngAdded = false;
    if (!anyHasPng)
    {
        if (!AppendMenuW(hSubMenu, MF_STRING, idCmdFirst + IDM_CONVERT_PNG, L"To PNG"))
        {
            DestroyMenu(hSubMenu);
            return HRESULT_FROM_WIN32(GetLastError());
        }
        pngAdded = true;
    }

	mii.hSubMenu = hSubMenu;

	if (!InsertMenuItem(hMenu, indexMenu, TRUE, &mii))
	{
		DestroyMenu(hSubMenu);
		return HRESULT_FROM_WIN32(GetLastError());
	}

	// Return an HRESULT value with the severity set to SEVERITY_SUCCESS. 
	// Set the code value to the offset of the largest command identifier 
	// that was assigned, plus one (1).
    USHORT largestId = static_cast<USHORT>(IDM_CONVERT_JPG);
    if (pngAdded)
    {
        largestId = static_cast<USHORT>(IDM_CONVERT_PNG);
    }

	return MAKE_HRESULT(SEVERITY_SUCCESS, 0, static_cast<USHORT>(largestId + 1));
}


//
//   FUNCTION: FileContextMenuExt::InvokeCommand
//
//   PURPOSE: This method is called when a user clicks a menu item to tell 
//            the handler to run the associated command. The lpcmi parameter 
//            points to a structure that contains the needed information.
//
IFACEMETHODIMP FileContextMenuExt::InvokeCommand(LPCMINVOKECOMMANDINFO pCommandInfo)
{
	// If the command cannot be identified through the verb string, then 
	// check the identifier offset.
	// Only support the submenu command offsets now.
	if (LOWORD(pCommandInfo->lpVerb) == IDM_CONVERT_JPG)
	{
		OnConvertToJpg(pCommandInfo->hwnd);
	}
	else if (LOWORD(pCommandInfo->lpVerb) == IDM_CONVERT_PNG)
	{
		OnConvertToPng(pCommandInfo->hwnd);
	}
	else
	{
		return E_FAIL;
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
	// No help or canonical verb exposed for a non-existent parent command.
	return E_INVALIDARG;
}

#pragma endregion
