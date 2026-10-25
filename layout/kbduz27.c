/*
 * Özbek klaviatura tartibi (2027-yilgi yangi alifbo: Ö, Ğ, Ş, Ç).
 *
 * Bitta manbadan ikki variant yig'iladi:
 *   (default)       KBDUZ27.DLL  - [ ] ; \ tugmalari ö ğ ş ç beradi
 *   -DUZ_US_VARIANT KBDUZ27U.DLL - US tartibi o'zgarmaydi, harflar faqat AltGr bilan
 *
 * Ikkala variantda ham AltGr+o/g/s/c -> ö/ğ/ş/ç, AltGr+' -> ʼ (tutuq),
 * AltGr+, -> «, AltGr+. -> ».
 *
 * Windows SDK (kbd.h) kerak bo'lmasligi uchun tuzilmalar shu yerda yozilgan.
 * WOW64 (SysWOW64) nusxasi -DBUILD_WOW6432 bilan yig'iladi: yadro jadvallarni
 * 64-bitli ko'rsatkichlar bilan o'qiydi.
 */

typedef unsigned char  BYTE;
typedef unsigned short USHORT;
typedef unsigned short WORD;
typedef unsigned short WCHAR;
typedef unsigned long  DWORD;

#if defined(BUILD_WOW6432)
#define KBD_LONG_POINTER __ptr64
#else
#define KBD_LONG_POINTER
#endif

#define NULL ((void *)0)

/* --- kbd.h dagi tuzilmalar ------------------------------------------------ */

typedef struct { BYTE Vk; BYTE ModBits; } VK_TO_BIT;

typedef struct {
    VK_TO_BIT *KBD_LONG_POINTER pVkToBit;
    WORD wMaxModBits;
    BYTE ModNumber[8];
} MODIFIERS;

typedef struct { BYTE VirtualKey; BYTE Attributes; WCHAR wch[1]; } VK_TO_WCHARS1;
typedef struct { BYTE VirtualKey; BYTE Attributes; WCHAR wch[6]; } VK_TO_WCHARS6;

typedef struct {
    VK_TO_WCHARS1 *KBD_LONG_POINTER pVkToWchars;
    BYTE nModifications;
    BYTE cbSize;
} VK_TO_WCHAR_TABLE;

typedef struct { BYTE vsc; WCHAR *KBD_LONG_POINTER pwsz; } VSC_LPWSTR;
typedef struct { BYTE Vsc; USHORT Vk; } VSC_VK;

typedef struct {
    MODIFIERS *KBD_LONG_POINTER pCharModifiers;
    VK_TO_WCHAR_TABLE *KBD_LONG_POINTER pVkToWcharTable;
    void *KBD_LONG_POINTER pDeadKey;
    VSC_LPWSTR *KBD_LONG_POINTER pKeyNames;
    VSC_LPWSTR *KBD_LONG_POINTER pKeyNamesExt;
    WCHAR *KBD_LONG_POINTER *KBD_LONG_POINTER pKeyNamesDead;
    USHORT *KBD_LONG_POINTER pusVSCtoVK;
    BYTE bMaxVSCtoVK;
    VSC_VK *KBD_LONG_POINTER pVSCtoVK_E0;
    VSC_VK *KBD_LONG_POINTER pVSCtoVK_E1;
    DWORD fLocaleFlags;
    BYTE nLgMax;
    BYTE cbLgEntry;
    void *KBD_LONG_POINTER pLigature;
    DWORD dwType;
    DWORD dwSubType;
} KBDTABLES;

#define KBDSHIFT     1
#define KBDCTRL      2
#define KBDALT       4
#define SHFT_INVALID 0x0F

#define KBDEXT       0x0100
#define KBDMULTIVK   0x0200
#define KBDSPECIAL   0x0400
#define KBDNUMPAD    0x0800

#define CAPLOK       0x01
#define CAPLOKALTGR  0x04

#define WCH_NONE     0xF000

#define KLLF_ALTGR   0x0001
#define KBD_VERSION  1
#define KEYBOARD_TYPE_GENERIC_101 4

/* --- Virtual key kodlari -------------------------------------------------- */

#define VK_CANCEL     0x03
#define VK_BACK       0x08
#define VK_TAB        0x09
#define VK_CLEAR      0x0C
#define VK_RETURN     0x0D
#define VK_SHIFT      0x10
#define VK_CONTROL    0x11
#define VK_MENU       0x12
#define VK_PAUSE      0x13
#define VK_CAPITAL    0x14
#define VK_ESCAPE     0x1B
#define VK_SPACE      0x20
#define VK_PRIOR      0x21
#define VK_NEXT       0x22
#define VK_END        0x23
#define VK_HOME       0x24
#define VK_LEFT       0x25
#define VK_UP         0x26
#define VK_RIGHT      0x27
#define VK_DOWN       0x28
#define VK_SNAPSHOT   0x2C
#define VK_INSERT     0x2D
#define VK_DELETE     0x2E
#define VK_HELP       0x2F
#define VK_LWIN       0x5B
#define VK_RWIN       0x5C
#define VK_APPS       0x5D
#define VK_SLEEP      0x5F
#define VK_NUMPAD0    0x60
#define VK_MULTIPLY   0x6A
#define VK_ADD        0x6B
#define VK_SUBTRACT   0x6D
#define VK_DECIMAL    0x6E
#define VK_DIVIDE     0x6F
#define VK_F1         0x70
#define VK_F11        0x7A
#define VK_F12        0x7B
#define VK_F13        0x7C
#define VK_F24        0x87
#define VK_NUMLOCK    0x90
#define VK_SCROLL     0x91
#define VK_LSHIFT     0xA0
#define VK_RSHIFT     0xA1
#define VK_LCONTROL   0xA2
#define VK_RCONTROL   0xA3
#define VK_LMENU      0xA4
#define VK_RMENU      0xA5
#define VK_BROWSER_BACK      0xA6
#define VK_BROWSER_FORWARD   0xA7
#define VK_BROWSER_REFRESH   0xA8
#define VK_BROWSER_STOP      0xA9
#define VK_BROWSER_SEARCH    0xAA
#define VK_BROWSER_FAVORITES 0xAB
#define VK_BROWSER_HOME      0xAC
#define VK_VOLUME_MUTE       0xAD
#define VK_VOLUME_DOWN       0xAE
#define VK_VOLUME_UP         0xAF
#define VK_MEDIA_NEXT_TRACK  0xB0
#define VK_MEDIA_PREV_TRACK  0xB1
#define VK_MEDIA_STOP        0xB2
#define VK_MEDIA_PLAY_PAUSE  0xB3
#define VK_LAUNCH_MAIL       0xB4
#define VK_LAUNCH_MEDIA_SELECT 0xB5
#define VK_LAUNCH_APP1       0xB6
#define VK_LAUNCH_APP2       0xB7
#define VK_OEM_1      0xBA
#define VK_OEM_PLUS   0xBB
#define VK_OEM_COMMA  0xBC
#define VK_OEM_MINUS  0xBD
#define VK_OEM_PERIOD 0xBE
#define VK_OEM_2      0xBF
#define VK_OEM_3      0xC0
#define VK_ABNT_C1    0xC1
#define VK_ABNT_C2    0xC2
#define VK_OEM_4      0xDB
#define VK_OEM_5      0xDC
#define VK_OEM_6      0xDD
#define VK_OEM_7      0xDE
#define VK_OEM_102    0xE2
#define VK_OEM_RESET  0xE9
#define VK_OEM_JUMP   0xEA
#define VK_OEM_PA1    0xEB
#define VK_OEM_PA2    0xEC
#define VK_OEM_PA3    0xED
#define VK_OEM_WSCTRL 0xEE
#define VK_OEM_FINISH 0xF1
#define VK_OEM_AUTO   0xF3
#define VK_OEM_BACKTAB 0xF5
#define VK_EREOF      0xF9
#define VK_ZOOM       0xFB
#define VK__none_     0xFF

/* --- Skan-kod -> VK (standart 101/102 tugmali klaviatura) ----------------- */

static USHORT ausVK[] = {
    VK__none_, VK_ESCAPE,
    '1', '2', '3', '4', '5', '6', '7', '8', '9', '0',
    VK_OEM_MINUS, VK_OEM_PLUS, VK_BACK, VK_TAB,
    'Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P',            /* 10-19 */
    VK_OEM_4, VK_OEM_6, VK_RETURN, VK_LCONTROL,                    /* 1A-1D */
    'A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L',                   /* 1E-26 */
    VK_OEM_1, VK_OEM_7, VK_OEM_3, VK_LSHIFT, VK_OEM_5,             /* 27-2B */
    'Z', 'X', 'C', 'V', 'B', 'N', 'M',                             /* 2C-32 */
    VK_OEM_COMMA, VK_OEM_PERIOD, VK_OEM_2,                         /* 33-35 */
    VK_RSHIFT | KBDEXT,                                            /* 36 */
    VK_MULTIPLY | KBDMULTIVK,                                      /* 37 */
    VK_LMENU, VK_SPACE, VK_CAPITAL,                                /* 38-3A */
    VK_F1, VK_F1 + 1, VK_F1 + 2, VK_F1 + 3, VK_F1 + 4,             /* 3B-3F */
    VK_F1 + 5, VK_F1 + 6, VK_F1 + 7, VK_F1 + 8, VK_F1 + 9,         /* 40-44 */
    VK_NUMLOCK | KBDEXT | KBDMULTIVK,                              /* 45 */
    VK_SCROLL | KBDMULTIVK,                                        /* 46 */
    VK_HOME   | KBDSPECIAL | KBDNUMPAD,                            /* 47 */
    VK_UP     | KBDSPECIAL | KBDNUMPAD,                            /* 48 */
    VK_PRIOR  | KBDSPECIAL | KBDNUMPAD,                            /* 49 */
    VK_SUBTRACT,                                                   /* 4A */
    VK_LEFT   | KBDSPECIAL | KBDNUMPAD,                            /* 4B */
    VK_CLEAR  | KBDSPECIAL | KBDNUMPAD,                            /* 4C */
    VK_RIGHT  | KBDSPECIAL | KBDNUMPAD,                            /* 4D */
    VK_ADD,                                                        /* 4E */
    VK_END    | KBDSPECIAL | KBDNUMPAD,                            /* 4F */
    VK_DOWN   | KBDSPECIAL | KBDNUMPAD,                            /* 50 */
    VK_NEXT   | KBDSPECIAL | KBDNUMPAD,                            /* 51 */
    VK_INSERT | KBDSPECIAL | KBDNUMPAD,                            /* 52 */
    VK_DELETE | KBDSPECIAL | KBDNUMPAD,                            /* 53 */
    VK_SNAPSHOT, VK__none_, VK_OEM_102, VK_F11, VK_F12,            /* 54-58 */
    VK_CLEAR, VK_OEM_WSCTRL, VK_OEM_FINISH, VK_OEM_JUMP,           /* 59-5C */
    VK_EREOF, VK_OEM_BACKTAB, VK_OEM_AUTO, VK__none_, VK__none_,   /* 5D-61 */
    VK_ZOOM, VK_HELP,                                              /* 62-63 */
    VK_F13, VK_F13 + 1, VK_F13 + 2, VK_F13 + 3, VK_F13 + 4,        /* 64-68 */
    VK_F13 + 5, VK_F13 + 6, VK_F13 + 7, VK_F13 + 8, VK_F13 + 9,    /* 69-6D */
    VK_F13 + 10, VK_OEM_PA3, VK__none_, VK_OEM_RESET, VK__none_,   /* 6E-72 */
    VK_ABNT_C1, VK__none_, VK__none_, VK_F24, VK__none_,           /* 73-77 */
    VK__none_, VK__none_, VK__none_, VK_OEM_PA1, VK_TAB,           /* 78-7C */
    VK__none_, VK_ABNT_C2, VK_OEM_PA2                              /* 7D-7F */
};

static VSC_VK aE0VscToVk[] = {
    { 0x10, VK_MEDIA_PREV_TRACK | KBDEXT },
    { 0x19, VK_MEDIA_NEXT_TRACK | KBDEXT },
    { 0x1D, VK_RCONTROL         | KBDEXT },
    { 0x20, VK_VOLUME_MUTE      | KBDEXT },
    { 0x21, VK_LAUNCH_APP2      | KBDEXT },
    { 0x22, VK_MEDIA_PLAY_PAUSE | KBDEXT },
    { 0x24, VK_MEDIA_STOP       | KBDEXT },
    { 0x2E, VK_VOLUME_DOWN      | KBDEXT },
    { 0x30, VK_VOLUME_UP        | KBDEXT },
    { 0x32, VK_BROWSER_HOME     | KBDEXT },
    { 0x35, VK_DIVIDE           | KBDEXT },
    { 0x37, VK_SNAPSHOT         | KBDEXT },
    { 0x38, VK_RMENU            | KBDEXT },
    { 0x47, VK_HOME             | KBDEXT },
    { 0x48, VK_UP               | KBDEXT },
    { 0x49, VK_PRIOR            | KBDEXT },
    { 0x4B, VK_LEFT             | KBDEXT },
    { 0x4D, VK_RIGHT            | KBDEXT },
    { 0x4F, VK_END              | KBDEXT },
    { 0x50, VK_DOWN             | KBDEXT },
    { 0x51, VK_NEXT             | KBDEXT },
    { 0x52, VK_INSERT           | KBDEXT },
    { 0x53, VK_DELETE           | KBDEXT },
    { 0x5B, VK_LWIN             | KBDEXT },
    { 0x5C, VK_RWIN             | KBDEXT },
    { 0x5D, VK_APPS             | KBDEXT },
    { 0x5F, VK_SLEEP            | KBDEXT },
    { 0x65, VK_BROWSER_SEARCH   | KBDEXT },
    { 0x66, VK_BROWSER_FAVORITES | KBDEXT },
    { 0x67, VK_BROWSER_REFRESH  | KBDEXT },
    { 0x68, VK_BROWSER_STOP     | KBDEXT },
    { 0x69, VK_BROWSER_FORWARD  | KBDEXT },
    { 0x6A, VK_BROWSER_BACK     | KBDEXT },
    { 0x6B, VK_LAUNCH_APP1      | KBDEXT },
    { 0x6C, VK_LAUNCH_MAIL      | KBDEXT },
    { 0x6D, VK_LAUNCH_MEDIA_SELECT | KBDEXT },
    { 0x1C, VK_RETURN           | KBDEXT },
    { 0x46, VK_CANCEL           | KBDEXT },
    { 0, 0 }
};

static VSC_VK aE1VscToVk[] = {
    { 0x1D, VK_PAUSE },
    { 0, 0 }
};

/* --- Modifikatorlar: 0 oddiy, 1 Shift, 2 Ctrl, 3 Shift+Ctrl, 4 AltGr, 5 Shift+AltGr */

static VK_TO_BIT aVkToBits[] = {
    { VK_SHIFT,   KBDSHIFT },
    { VK_CONTROL, KBDCTRL  },
    { VK_MENU,    KBDALT   },
    { 0, 0 }
};

static MODIFIERS CharModifiers = {
    &aVkToBits[0],
    7,
    { 0, 1, 2, 3, SHFT_INVALID, SHFT_INVALID, 4, 5 }
};

/* --- Belgilar ------------------------------------------------------------- */

#define NONE WCH_NONE
#define O_UML  0x00F6  /* ö */
#define O_UMLU 0x00D6  /* Ö */
#define G_BRV  0x011F  /* ğ */
#define G_BRVU 0x011E  /* Ğ */
#define S_CED  0x015F  /* ş */
#define S_CEDU 0x015E  /* Ş */
#define C_CED  0x00E7  /* ç */
#define C_CEDU 0x00C7  /* Ç */
#define TUTUQ  0x02BC  /* ʼ */

#define LETTER(ch, lo, ctl) { ch, CAPLOK, { lo, ch, ctl, NONE, NONE, NONE } }
#define LETTER_AG(ch, lo, ctl, ag, agu) \
    { ch, CAPLOK | CAPLOKALTGR, { lo, ch, ctl, NONE, ag, agu } }

static VK_TO_WCHARS6 aVkToWch6[] = {
    LETTER('A', 'a', 0x01), LETTER('B', 'b', 0x02),
    LETTER_AG('C', 'c', 0x03, C_CED, C_CEDU),
    LETTER('D', 'd', 0x04), LETTER('E', 'e', 0x05), LETTER('F', 'f', 0x06),
    LETTER_AG('G', 'g', 0x07, G_BRV, G_BRVU),
    LETTER('H', 'h', 0x08), LETTER('I', 'i', 0x09), LETTER('J', 'j', 0x0A),
    LETTER('K', 'k', 0x0B), LETTER('L', 'l', 0x0C), LETTER('M', 'm', 0x0D),
    LETTER('N', 'n', 0x0E),
    LETTER_AG('O', 'o', 0x0F, O_UML, O_UMLU),
    LETTER('P', 'p', 0x10), LETTER('Q', 'q', 0x11), LETTER('R', 'r', 0x12),
    LETTER_AG('S', 's', 0x13, S_CED, S_CEDU),
    LETTER('T', 't', 0x14), LETTER('U', 'u', 0x15), LETTER('V', 'v', 0x16),
    LETTER('W', 'w', 0x17), LETTER('X', 'x', 0x18), LETTER('Y', 'y', 0x19),
    LETTER('Z', 'z', 0x1A),

    { '1', 0, { '1', '!',  NONE, NONE, NONE, NONE } },
    { '2', 0, { '2', '@',  NONE, 0x00, NONE, NONE } },
    { '3', 0, { '3', '#',  NONE, NONE, NONE, NONE } },
    { '4', 0, { '4', '$',  NONE, NONE, NONE, NONE } },
    { '5', 0, { '5', '%',  NONE, NONE, NONE, NONE } },
    { '6', 0, { '6', '^',  NONE, 0x1E, NONE, NONE } },
    { '7', 0, { '7', '&',  NONE, NONE, NONE, NONE } },
    { '8', 0, { '8', '*',  NONE, NONE, NONE, NONE } },
    { '9', 0, { '9', '(',  NONE, NONE, NONE, NONE } },
    { '0', 0, { '0', ')',  NONE, NONE, NONE, NONE } },
    { VK_OEM_MINUS,  0, { '-', '_', NONE, 0x1F, NONE, NONE } },
    { VK_OEM_PLUS,   0, { '=', '+', NONE, NONE, NONE, NONE } },

#if defined(UZ_US_VARIANT)
    { VK_OEM_4, 0, { '[',  '{', 0x1B, NONE, NONE, NONE } },
    { VK_OEM_6, 0, { ']',  '}', 0x1D, NONE, NONE, NONE } },
    { VK_OEM_1, 0, { ';',  ':', NONE, NONE, NONE, NONE } },
    { VK_OEM_5, 0, { '\\', '|', 0x1C, NONE, NONE, NONE } },
#else
    { VK_OEM_4, CAPLOK, { O_UML, O_UMLU, 0x1B, NONE, '[',  '{' } },
    { VK_OEM_6, CAPLOK, { G_BRV, G_BRVU, 0x1D, NONE, ']',  '}' } },
    { VK_OEM_1, CAPLOK, { S_CED, S_CEDU, NONE, NONE, ';',  ':' } },
    { VK_OEM_5, CAPLOK, { C_CED, C_CEDU, 0x1C, NONE, '\\', '|' } },
#endif

    { VK_OEM_7,      0, { '\'', '"', NONE, NONE, TUTUQ,  NONE } },
    { VK_OEM_3,      0, { '`',  '~', NONE, NONE, NONE,   NONE } },
    { VK_OEM_COMMA,  0, { ',',  '<', NONE, NONE, 0x00AB, NONE } },
    { VK_OEM_PERIOD, 0, { '.',  '>', NONE, NONE, 0x00BB, NONE } },
    { VK_OEM_2,      0, { '/',  '?', NONE, NONE, NONE,   NONE } },
    { VK_OEM_102,    0, { '\\', '|', 0x1C, NONE, NONE,   NONE } },

    { VK_BACK,     0, { '\b', '\b', 0x7F, NONE, NONE, NONE } },
    { VK_ESCAPE,   0, { 0x1B, 0x1B, 0x1B, NONE, NONE, NONE } },
    { VK_RETURN,   0, { '\r', '\r', '\n', NONE, NONE, NONE } },
    { VK_CANCEL,   0, { 0x03, 0x03, 0x03, NONE, NONE, NONE } },
    { VK_SPACE,    0, { ' ',  ' ',  ' ',  NONE, ' ',  ' '  } },
    { VK_TAB,      0, { '\t', '\t', NONE, NONE, NONE, NONE } },
    { VK_ADD,      0, { '+',  '+',  NONE, NONE, NONE, NONE } },
    { VK_DIVIDE,   0, { '/',  '/',  NONE, NONE, NONE, NONE } },
    { VK_MULTIPLY, 0, { '*',  '*',  NONE, NONE, NONE, NONE } },
    { VK_SUBTRACT, 0, { '-',  '-',  NONE, NONE, NONE, NONE } },
    { 0, 0, { 0, 0, 0, 0, 0, 0 } }
};

/* Numpad oxirida turishi kerak: VkKeyScan raqamlarni asosiy qatordan olsin. */
static VK_TO_WCHARS1 aVkToWch1[] = {
    { VK_NUMPAD0 + 0, 0, { '0' } }, { VK_NUMPAD0 + 1, 0, { '1' } },
    { VK_NUMPAD0 + 2, 0, { '2' } }, { VK_NUMPAD0 + 3, 0, { '3' } },
    { VK_NUMPAD0 + 4, 0, { '4' } }, { VK_NUMPAD0 + 5, 0, { '5' } },
    { VK_NUMPAD0 + 6, 0, { '6' } }, { VK_NUMPAD0 + 7, 0, { '7' } },
    { VK_NUMPAD0 + 8, 0, { '8' } }, { VK_NUMPAD0 + 9, 0, { '9' } },
    { VK_DECIMAL,     0, { '.' } },
    { 0, 0, { 0 } }
};

static VK_TO_WCHAR_TABLE aVkToWcharTable[] = {
    { (VK_TO_WCHARS1 *)aVkToWch6, 6, sizeof(aVkToWch6[0]) },
    { (VK_TO_WCHARS1 *)aVkToWch1, 1, sizeof(aVkToWch1[0]) },
    { NULL, 0, 0 }
};

/* --- Tugma nomlari (GetKeyNameText uchun) --------------------------------- */

static VSC_LPWSTR aKeyNames[] = {
    { 0x01, L"Esc" },        { 0x0E, L"Backspace" },  { 0x0F, L"Tab" },
    { 0x1C, L"Enter" },      { 0x1D, L"Ctrl" },       { 0x2A, L"Shift" },
    { 0x36, L"Right Shift" },{ 0x37, L"Num *" },      { 0x38, L"Alt" },
    { 0x39, L"Space" },      { 0x3A, L"Caps Lock" },
    { 0x3B, L"F1" }, { 0x3C, L"F2" }, { 0x3D, L"F3" }, { 0x3E, L"F4" },
    { 0x3F, L"F5" }, { 0x40, L"F6" }, { 0x41, L"F7" }, { 0x42, L"F8" },
    { 0x43, L"F9" }, { 0x44, L"F10" },
    { 0x45, L"Pause" },      { 0x46, L"Scroll Lock" },
    { 0x47, L"Num 7" }, { 0x48, L"Num 8" }, { 0x49, L"Num 9" }, { 0x4A, L"Num -" },
    { 0x4B, L"Num 4" }, { 0x4C, L"Num 5" }, { 0x4D, L"Num 6" }, { 0x4E, L"Num +" },
    { 0x4F, L"Num 1" }, { 0x50, L"Num 2" }, { 0x51, L"Num 3" }, { 0x52, L"Num 0" },
    { 0x53, L"Num Del" },    { 0x54, L"Sys Req" },
    { 0x57, L"F11" },        { 0x58, L"F12" },
    { 0x7C, L"F13" }, { 0x7D, L"F14" }, { 0x7E, L"F15" }, { 0x7F, L"F16" },
    { 0, NULL }
};

static VSC_LPWSTR aKeyNamesExt[] = {
    { 0x1C, L"Num Enter" },     { 0x1D, L"Right Ctrl" },  { 0x35, L"Num /" },
    { 0x37, L"Prnt Scrn" },     { 0x38, L"Right Alt" },   { 0x45, L"Num Lock" },
    { 0x46, L"Break" },         { 0x47, L"Home" },        { 0x48, L"Up" },
    { 0x49, L"Page Up" },       { 0x4B, L"Left" },        { 0x4D, L"Right" },
    { 0x4F, L"End" },           { 0x50, L"Down" },        { 0x51, L"Page Down" },
    { 0x52, L"Insert" },        { 0x53, L"Delete" },      { 0x56, L"Help" },
    { 0x5B, L"Left Windows" },  { 0x5C, L"Right Windows" }, { 0x5D, L"Application" },
    { 0, NULL }
};

static KBDTABLES KbdTables = {
    &CharModifiers,
    aVkToWcharTable,
    NULL,                       /* o'lik tugmalar yo'q */
    aKeyNames,
    aKeyNamesExt,
    NULL,
    ausVK,
    sizeof(ausVK) / sizeof(ausVK[0]),
    aE0VscToVk,
    aE1VscToVk,
    (KBD_VERSION << 16) | KLLF_ALTGR,
    0, 0, NULL,                 /* ligaturalar yo'q */
    KEYBOARD_TYPE_GENERIC_101,
    0
};

__declspec(dllexport) KBDTABLES *KBD_LONG_POINTER KbdLayerDescriptor(void)
{
    return &KbdTables;
}
