$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$utf8 = New-Object System.Text.UTF8Encoding($false)
# Generated translation-unit numbers change between SDK builds. Find each
# function by its address instead of depending on a partition number.
$functionFiles = @{}
Get-ChildItem -LiteralPath (Join-Path $root 'generated/default') -Filter '*_recomp.*.cpp' | ForEach-Object {
    $file = $_.FullName
    foreach ($match in [regex]::Matches([IO.File]::ReadAllText($file), 'DEFINE_REX_FUNC\((sub_[0-9A-F]+)\)')) {
        $functionFiles[$match.Groups[1].Value] = $file
    }
}

function Update-GeneratedFile($relativePath, $functionName, $original, $patched) {
    $path = $functionFiles[$functionName]
    if (-not $path) {
        throw "Generated file missing: $relativePath. Run ReXGlue codegen first."
    }
    $content = [System.IO.File]::ReadAllText($path)
    $start = $content.IndexOf("DEFINE_REX_FUNC($functionName)")
    if ($start -lt 0) { throw "Function $functionName missing from $relativePath" }
    $end = $content.IndexOf('DEFINE_REX_FUNC(', $start + 1)
    if ($end -lt 0) { $end = $content.Length }
    $function = $content.Substring($start, $end - $start)
    if ($function.Contains($patched)) { return }
    $matches = 0
    foreach ($knownOriginal in @($original)) {
        $count = ([regex]::Matches($function, [regex]::Escape($knownOriginal))).Count
        if ($count -eq 1) {
            $function = $function.Replace($knownOriginal, $patched)
        }
        $matches += $count
    }
    if ($matches -ne 1) {
        throw "Expected exactly one known instruction in $relativePath; found $matches. Check the game version."
    }
    $content = $content.Substring(0, $start) + $function + $content.Substring($end)
    $declarations = @"
bool Pgr4Enable60Fps();
bool Pgr4DisableHdr();
bool Pgr4DisableMotionBlur();
void Pgr4FrameWaitYield();
void Pgr4LogCarPreviewChainLimit(uint32_t, uint32_t);
void Pgr4LogCarPreviewCellLimit(uint32_t, uint32_t);
"@
    if (!$content.Contains('bool Pgr4Enable60Fps();')) {
        $content = $content.Insert($content.IndexOf("`n") + 1, $declarations + "`n")
    }
    [System.IO.File]::WriteAllText($path, $content, $utf8)
}

$fpsOriginal = "loc_826957EC:`n`t// lwz r10,13580(r31)`n`tctx.r10.u64 = REX_LOAD_U32(ctx.r31.u32 + 13580);"
$fpsPatched = "loc_826957EC:`n`t// Optional 60 FPS patch: 0x826957EC, lwz r10,13580(r31) -> li r10,1.`n`tctx.r10.u64 = Pgr4Enable60Fps() ? 1 : REX_LOAD_U32(ctx.r31.u32 + 13580);"
$hdrOriginal = "`t// stw r11,144(r1)`n`tREX_STORE_U32(ctx.r1.u32 + 144, ctx.r11.u32);`n`t// li r11,1`n`tctx.r11.s64 = 1;`n`t// stw r31,140(r1)"
$hdrPatched = "`t// stw r11,144(r1)`n`tREX_STORE_U32(ctx.r1.u32 + 144, ctx.r11.u32);`n`t// Optional upscale flicker patch: 0x829A89EC, li r11,1 -> li r11,0.`n`tctx.r11.s64 = Pgr4DisableHdr() ? 0 : 1;`n`t// stw r31,140(r1)"

$fpsFile = 'generated/default/pgr4_recompiled_recomp.22.cpp'
$hdrFile = 'generated/default/pgr4_recompiled_recomp.110.cpp'
$frameWaitFile = 'generated/default/pgr4_recompiled_recomp.16.cpp'
$boneDecodeFile = 'generated/default/pgr4_recompiled_recomp.69.cpp'
$motionBlurFile = 'generated/default/pgr4_recompiled_recomp.123.cpp'
Update-GeneratedFile $fpsFile 'sub_82695738' $fpsOriginal $fpsPatched
Update-GeneratedFile $hdrFile 'sub_829A8960' $hdrOriginal $hdrPatched
$motionBlurOriginal = "`t// li r11,1`n`tctx.r11.s64 = 1;`n`t// stw r31,140(r1)"
$motionBlurPatched = "`t// Optional motion blur patch: 0x829A9A9F, li r11,1 -> li r11,0.`n`tctx.r11.s64 = Pgr4DisableMotionBlur() ? 0 : 1;`n`t// stw r31,140(r1)"
Update-GeneratedFile $motionBlurFile 'sub_829A9A10' $motionBlurOriginal $motionBlurPatched

# This game-frame polling loop contains db16cyc instructions. The recompiler
# treats those as comments, so offer an opt-in host yield during the wait.
$frameWaitOriginal = "loc_8269798C:`n`t// db16cyc"
$frameWaitPatched = "loc_8269798C:`n`tPgr4FrameWaitYield();`n`t// db16cyc"
Update-GeneratedFile $frameWaitFile 'sub_82697970' $frameWaitOriginal $frameWaitPatched

# VMX128 NORMPACKED64 unpack adds signed fields to the float bit patterns
# for 3.0 (xyz) and 1.0 (w). Numeric float conversion corrupts bone poses.
# Keep this generated-code patch until the local ReXGlue codegen is rebuilt
# with the corresponding fix in src/codegen/builders/vector.cpp.
foreach ($reg in @('v10', 'v6')) {
    $original = @"
	vTemp.u64[0] = ctx.$reg.u64[1];
	temp.s32 = (int32_t(vTemp.u64[0] << 44) >> 44);
	ctx.$reg.f32[0] = float(temp.s32);
	temp.s32 = (int32_t(vTemp.u64[0] << 24) >> 44);
	ctx.$reg.f32[1] = float(temp.s32);
	temp.s32 = (int32_t(vTemp.u64[0] << 4) >> 44);
	ctx.$reg.f32[2] = float(temp.s32);
	ctx.$reg.f32[3] = float(vTemp.u64[0] >> 60);
"@.TrimEnd()
    $patched = @"
	vTemp.u64[0] = ctx.$reg.u64[0];
	temp.s32 = int32_t(int64_t(vTemp.u64[0] << 44) >> 44);
	ctx.$reg.u32[3] = 0x40400000u + uint32_t(temp.s32);
	temp.s32 = int32_t(int64_t(vTemp.u64[0] << 24) >> 44);
	ctx.$reg.u32[2] = 0x40400000u + uint32_t(temp.s32);
	temp.s32 = int32_t(int64_t(vTemp.u64[0] << 4) >> 44);
	ctx.$reg.u32[1] = 0x40400000u + uint32_t(temp.s32);
	ctx.$reg.u32[0] = 0x3F800000u + uint32_t(vTemp.u64[0] >> 60);
"@.TrimEnd()
    $previous = $patched.Replace("ctx.$reg.u64[0];", "ctx.$reg.u64[1];")
    Update-GeneratedFile $boneDecodeFile 'sub_823BD9D8' @($original, $previous) $patched
}

# The Lotus Esprit preview repeatedly traverses this relative-offset list in
# sub_822A53E0. A captured frozen thread was inside the loc_822A5520 loop.
# Bound each cell's traversal so malformed/cyclic data cannot stall the game.
$chainLocalOriginal = "`tuint32_t ea{};`n`t// mflr r12"
$chainLocalPatched = "`tuint32_t ea{};`n`tuint32_t pgr4_chain_steps = 0;`n`tuint32_t pgr4_cell_steps = 0;`n`t// mflr r12"
$chainCellOriginal = "loc_822A54DC:`n`t// lwz r9,0(r29)"
$chainCellPatched = "loc_822A54DC:`n`tif (++pgr4_cell_steps > 1024) {`n`t`tPgr4LogCarPreviewCellLimit(ctx.r30.u32, ctx.r31.u32);`n`t`tgoto loc_822A5588;`n`t}`n`t// lwz r9,0(r29)"
$chainStartOriginal = "`t// beq cr6,0x822a5570`n`tif (ctx.cr6.eq) goto loc_822A5570;`nloc_822A5520:"
$chainStartPatched = "`t// beq cr6,0x822a5570`n`tif (ctx.cr6.eq) goto loc_822A5570;`n`t pgr4_chain_steps = 0;`nloc_822A5520:"
$chainLoopOriginal = "loc_822A5520:`n`t// lwz r11,4(r3)"
$chainLoopPatched = "loc_822A5520:`n`tif (++pgr4_chain_steps > 4096) {`n`t`tPgr4LogCarPreviewChainLimit(ctx.r3.u32, ctx.r10.u32);`n`t`tgoto loc_822A5570;`n`t}`n`t// lwz r11,4(r3)"
Update-GeneratedFile $hdrFile 'sub_822A53E0' $chainLocalOriginal $chainLocalPatched
Update-GeneratedFile $hdrFile 'sub_822A53E0' $chainCellOriginal $chainCellPatched
Update-GeneratedFile $hdrFile 'sub_822A53E0' $chainStartOriginal $chainStartPatched
Update-GeneratedFile $hdrFile 'sub_822A53E0' $chainLoopOriginal $chainLoopPatched

# Garage collision trace: record the exact proposed and returned position at
# the walking camera callback. Disabled unless explicitly requested.
$walkCallOriginal = "`tctx.lr = 0x827A7510;`n`tREX_CALL_INDIRECT_FUNC(ctx.ctr.u32);"
$walkCallPatched = @"
`tctx.lr = 0x827A7510;
`textern void Pgr4TraceGarageWalk(uint8_t*, uint32_t, uint32_t, uint32_t, uint32_t, uint32_t);
`tPgr4TraceGarageWalk(base, ctx.r29.u32, 0, ctx.r1.u32 + 128, ctx.r1.u32 + 144, 0);
`tREX_CALL_INDIRECT_FUNC(ctx.ctr.u32);
`tPgr4TraceGarageWalk(base, ctx.r29.u32, 1, 0, 0, ctx.r1.u32 + 128);
"@.TrimEnd()
Update-GeneratedFile 'generated/default/pgr4_recompiled_recomp.85.cpp' 'sub_827A6B90' $walkCallOriginal $walkCallPatched
# 60 FPS walking requests are shorter than 5cm. This collision callback uses
# trunc(distance * 20), then skips all movement when the count becomes zero.
# Preserve its early near-zero rejection and normal collision resolution.
$garageCallerOriginal = "`tuint32_t ea{};`n`t// mflr r12"
$garageCallerPatched = "`tuint32_t ea{};`n`tconst uint32_t pgr4_walk_caller = uint32_t(ctx.lr);`n`t// mflr r12"
$garageCountOriginal = "`t// lwz r11,80(r1)`n`tctx.r11.u64 = REX_LOAD_U32(ctx.r1.u32 + 80);`n`t// extsw r10,r11"
$garageCountPatched = @"
`t// lwz r11,80(r1)
`tctx.r11.u64 = REX_LOAD_U32(ctx.r1.u32 + 80);
`textern int32_t Pgr4GarageCollisionSteps(int32_t, uint32_t);
`tctx.r11.s64 = Pgr4GarageCollisionSteps(ctx.r11.s32, pgr4_walk_caller);
`t// extsw r10,r11
"@.TrimEnd()
Update-GeneratedFile 'generated/default/pgr4_recompiled_recomp.58.cpp' 'sub_82499578' $garageCallerOriginal $garageCallerPatched
Update-GeneratedFile 'generated/default/pgr4_recompiled_recomp.58.cpp' 'sub_82499578' $garageCountOriginal $garageCountPatched