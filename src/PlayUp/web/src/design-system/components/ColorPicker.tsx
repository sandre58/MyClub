import {
  useEffect,
  useId,
  useLayoutEffect,
  useRef,
  useState,
  type CSSProperties,
  type PointerEvent as ReactPointerEvent,
} from 'react'
import { createPortal } from 'react-dom'
import { CloseIcon } from '../icons/shellIcons'
import { PipetteIcon } from '../icons/overviewIcons'
import { TextInput } from './TextInput'
import { InputNumber } from './InputNumber'
import { Select } from './Select'
import {
  HEX6,
  clamp01,
  clampHue,
  defaultHsv,
  hexToHsv,
  hsvToHex,
  hsvToRgb,
  hueCss,
  normalizeHex,
  rgbToHsv,
  type ColorFormat,
  type Hsv,
  type Rgb,
} from '../colorMath'

export type ColorPickerProps = {
  value: string
  onChange: (next: string) => void
  disabled?: boolean
  placeholder?: string
  allowClear?: boolean
  clearLabel?: string
  eyedropperLabel?: string
  'aria-label'?: string
  id?: string
}

type EyeDropperCtor = new () => {
  open: (options?: { signal?: AbortSignal }) => Promise<{ sRGBHex: string }>
}

function getEyeDropper(): EyeDropperCtor | null {
  const ctor = (window as Window & { EyeDropper?: EyeDropperCtor }).EyeDropper
  return ctor ?? null
}

const FORMAT_OPTIONS = [
  { value: 'hex', label: 'HEX' },
  { value: 'rgb', label: 'RGB' },
  { value: 'hsb', label: 'HSB' },
]

/**
 * Color control — Input shell + HSV editor panel (Ant-inspired).
 * Opaque #RRGGBB. Formats HEX / RGB / HSB. Optional clear on the trigger.
 */
export function ColorPicker({
  value,
  onChange,
  disabled = false,
  placeholder = '#RRGGBB',
  allowClear = true,
  clearLabel = 'Vider',
  eyedropperLabel = 'Pipette',
  id,
  'aria-label': ariaLabel = 'Couleur',
}: ColorPickerProps) {
  const autoId = useId()
  const triggerId = id ?? autoId
  const panelId = `${triggerId}-panel`
  const rootRef = useRef<HTMLDivElement>(null)
  const panelRef = useRef<HTMLDivElement>(null)
  const svRef = useRef<HTMLDivElement>(null)
  const hueRef = useRef<HTMLDivElement>(null)
  const hsvRef = useRef<Hsv>(defaultHsv())
  const pickingRef = useRef(false)

  const [open, setOpen] = useState(false)
  const [panelStyle, setPanelStyle] = useState<CSSProperties | undefined>()
  const [format, setFormat] = useState<ColorFormat>('hex')
  const [hexDraft, setHexDraft] = useState(value)
  const [hsv, setHsv] = useState<Hsv>(() => {
    const initial = hexToHsv(value, defaultHsv().h) ?? defaultHsv()
    hsvRef.current = initial
    return initial
  })
  const [eyedropperSupported, setEyedropperSupported] = useState(false)

  useEffect(() => {
    setEyedropperSupported(getEyeDropper() !== null)
  }, [])

  useEffect(() => {
    setHexDraft(value)
    const next = hexToHsv(value.trim(), hsvRef.current.h)
    if (next) {
      hsvRef.current = next
      setHsv(next)
    }
  }, [value])

  useLayoutEffect(() => {
    if (!open) {
      setPanelStyle(undefined)
      return
    }

    function placePanel() {
      const anchor = rootRef.current
      if (!anchor) {
        return
      }
      const rect = anchor.getBoundingClientRect()
      const gap = 8
      const panelWidth = Math.min(window.innerWidth - 32, 18.5 * 16)
      let left = rect.left
      if (left + panelWidth > window.innerWidth - 16) {
        left = Math.max(16, window.innerWidth - 16 - panelWidth)
      }
      const spaceBelow = window.innerHeight - rect.bottom - gap
      const preferBelow = spaceBelow >= 280 || spaceBelow >= rect.top
      setPanelStyle({
        position: 'fixed',
        top: preferBelow ? rect.bottom + gap : undefined,
        bottom: preferBelow ? undefined : window.innerHeight - rect.top + gap,
        left,
        width: panelWidth,
        zIndex: 50,
      })
    }

    placePanel()
    window.addEventListener('resize', placePanel)
    window.addEventListener('scroll', placePanel, true)
    return () => {
      window.removeEventListener('resize', placePanel)
      window.removeEventListener('scroll', placePanel, true)
    }
  }, [open])

  useEffect(() => {
    if (!open) {
      return
    }

    function onPointerDown(event: MouseEvent) {
      if (pickingRef.current) {
        return
      }
      const target = event.target as Node
      if (
        rootRef.current?.contains(target) ||
        panelRef.current?.contains(target)
      ) {
        return
      }
      setOpen(false)
    }

    function onKeyDown(event: KeyboardEvent) {
      if (pickingRef.current) {
        return
      }
      if (event.key === 'Escape') {
        if (
          panelRef.current?.querySelector(
            '.ds-select__shell[data-open="true"]',
          )
        ) {
          return
        }
        setOpen(false)
      }
    }

    document.addEventListener('mousedown', onPointerDown)
    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('mousedown', onPointerDown)
      document.removeEventListener('keydown', onKeyDown)
    }
  }, [open])

  const normalized = value.trim()
  const valid = HEX6.test(normalized)
  const empty = normalized.length === 0
  const display = empty ? placeholder : normalized.toUpperCase()
  const liveHex = hsvToHex(hsv)
  const hueFill = hueCss(hsv.h)
  const rgb = hsvToRgb(hsv)
  const showClear = allowClear && !empty && !disabled

  function commitHsv(next: Hsv) {
    const clamped: Hsv = {
      h: clampHue(next.h),
      s: clamp01(next.s),
      v: clamp01(next.v),
    }
    hsvRef.current = clamped
    setHsv(clamped)
    const hex = hsvToHex(clamped)
    setHexDraft(hex)
    onChange(hex)
  }

  function commitHex(raw: string) {
    const next = raw.trim()
    if (next.length === 0) {
      onChange('')
      return
    }
    const normalizedHex = normalizeHex(next)
    if (normalizedHex) {
      const parsed = hexToHsv(normalizedHex, hsvRef.current.h)
      if (parsed) {
        hsvRef.current = parsed
        setHsv(parsed)
      }
      setHexDraft(normalizedHex)
      onChange(normalizedHex)
    }
  }

  function commitRgb(next: Rgb) {
    commitHsv(rgbToHsv(next, hsvRef.current.h))
  }

  function clearValue() {
    setHexDraft('')
    onChange('')
  }

  async function onEyedropper() {
    const EyeDropper = getEyeDropper()
    if (!EyeDropper || disabled) {
      return
    }

    pickingRef.current = true
    try {
      const result = await new EyeDropper().open()
      const hex = normalizeHex(result.sRGBHex)
      if (hex) {
        commitHex(hex)
      }
    } catch {
      // User cancelled (Escape) or API aborted — keep panel open.
    } finally {
      pickingRef.current = false
    }
  }

  function readSvFromPointer(clientX: number, clientY: number) {
    const el = svRef.current
    if (!el) {
      return
    }
    const rect = el.getBoundingClientRect()
    if (rect.width <= 0 || rect.height <= 0) {
      return
    }
    const s = clamp01((clientX - rect.left) / rect.width)
    const v = clamp01(1 - (clientY - rect.top) / rect.height)
    commitHsv({ ...hsvRef.current, s, v })
  }

  function readHueFromPointer(clientX: number) {
    const el = hueRef.current
    if (!el) {
      return
    }
    const rect = el.getBoundingClientRect()
    if (rect.width <= 0) {
      return
    }
    const h = clampHue(((clientX - rect.left) / rect.width) * 360)
    commitHsv({ ...hsvRef.current, h })
  }

  function bindDrag(
    event: ReactPointerEvent<HTMLElement>,
    onMove: (clientX: number, clientY: number) => void,
  ) {
    event.preventDefault()
    const target = event.currentTarget
    target.setPointerCapture(event.pointerId)
    onMove(event.clientX, event.clientY)

    function onPointerMove(move: PointerEvent) {
      onMove(move.clientX, move.clientY)
    }

    function onPointerUp(up: PointerEvent) {
      target.releasePointerCapture(up.pointerId)
      target.removeEventListener('pointermove', onPointerMove)
      target.removeEventListener('pointerup', onPointerUp)
      target.removeEventListener('pointercancel', onPointerUp)
    }

    target.addEventListener('pointermove', onPointerMove)
    target.addEventListener('pointerup', onPointerUp)
    target.addEventListener('pointercancel', onPointerUp)
  }

  return (
    <div className="ds-color-picker" ref={rootRef}>
      <div
        className="ds-input ds-color-picker__shell"
        data-disabled={disabled ? 'true' : 'false'}
        data-open={open ? 'true' : 'false'}
      >
        <button
          type="button"
          id={triggerId}
          className="ds-color-picker__trigger"
          disabled={disabled}
          aria-label={ariaLabel}
          aria-expanded={open}
          aria-haspopup="dialog"
          aria-controls={open ? panelId : undefined}
          onClick={() => {
            if (!disabled) {
              setOpen((current) => !current)
            }
          }}
        >
          <span
            className="ds-color-picker__swatch"
            data-empty={empty || !valid ? 'true' : 'false'}
            style={
              valid
                ? ({ ['--ds-color-swatch' as string]: normalized } as object)
                : undefined
            }
          />
          <span
            className="ds-color-picker__value"
            data-empty={empty ? 'true' : 'false'}
          >
            {display}
          </span>
        </button>
        {showClear ? (
          <button
            type="button"
            className="ds-input__affix"
            aria-label={clearLabel}
            title={clearLabel}
            tabIndex={-1}
            onClick={(event) => {
              event.stopPropagation()
              clearValue()
            }}
          >
            <CloseIcon size="sm" aria-hidden="true" />
          </button>
        ) : null}
      </div>

      {open && panelStyle
        ? createPortal(
            <div
              ref={panelRef}
              id={panelId}
              className="ds-color-picker__panel"
              data-portaled="true"
              role="dialog"
              aria-label={ariaLabel}
              style={panelStyle}
            >
          <div
            ref={svRef}
            className="ds-color-picker__sv"
            style={{ ['--ds-color-hue' as string]: hueFill }}
            role="slider"
            aria-label="Saturation et luminosité"
            aria-valuemin={0}
            aria-valuemax={100}
            aria-valuetext={`Saturation ${Math.round(hsv.s * 100)} %, luminosité ${Math.round(hsv.v * 100)} %`}
            tabIndex={disabled ? -1 : 0}
            onPointerDown={(event) => {
              if (!disabled) {
                bindDrag(event, readSvFromPointer)
              }
            }}
            onKeyDown={(event) => {
              if (disabled) {
                return
              }
              const step = event.shiftKey ? 0.1 : 0.02
              if (event.key === 'ArrowRight') {
                event.preventDefault()
                commitHsv({ ...hsv, s: hsv.s + step })
              } else if (event.key === 'ArrowLeft') {
                event.preventDefault()
                commitHsv({ ...hsv, s: hsv.s - step })
              } else if (event.key === 'ArrowUp') {
                event.preventDefault()
                commitHsv({ ...hsv, v: hsv.v + step })
              } else if (event.key === 'ArrowDown') {
                event.preventDefault()
                commitHsv({ ...hsv, v: hsv.v - step })
              }
            }}
          >
            <span
              className="ds-color-picker__sv-thumb"
              style={{
                left: `${hsv.s * 100}%`,
                top: `${(1 - hsv.v) * 100}%`,
                background: liveHex,
              }}
            />
          </div>

          <div className="ds-color-picker__slider-row">
            <div className="ds-color-picker__sliders">
              <div
                ref={hueRef}
                className="ds-color-picker__hue"
                role="slider"
                aria-label="Teinte"
                aria-valuemin={0}
                aria-valuemax={360}
                aria-valuenow={Math.round(hsv.h)}
                tabIndex={disabled ? -1 : 0}
                onPointerDown={(event) => {
                  if (!disabled) {
                    bindDrag(event, (x) => readHueFromPointer(x))
                  }
                }}
                onKeyDown={(event) => {
                  if (disabled) {
                    return
                  }
                  const step = event.shiftKey ? 10 : 2
                  if (event.key === 'ArrowRight' || event.key === 'ArrowUp') {
                    event.preventDefault()
                    commitHsv({ ...hsv, h: hsv.h + step })
                  } else if (
                    event.key === 'ArrowLeft' ||
                    event.key === 'ArrowDown'
                  ) {
                    event.preventDefault()
                    commitHsv({ ...hsv, h: hsv.h - step })
                  }
                }}
              >
                <span
                  className="ds-color-picker__hue-thumb"
                  style={{
                    left: `${(hsv.h / 360) * 100}%`,
                    background: hueFill,
                  }}
                />
              </div>
            </div>
            <span
              className="ds-color-picker__preview"
              style={{ background: liveHex }}
              aria-hidden="true"
            />
            {eyedropperSupported ? (
              <button
                type="button"
                className="ds-color-picker__eyedropper"
                disabled={disabled}
                aria-label={eyedropperLabel}
                title={eyedropperLabel}
                onClick={() => {
                  void onEyedropper()
                }}
              >
                <PipetteIcon size="sm" aria-hidden="true" />
              </button>
            ) : null}
          </div>

          <div className="ds-color-picker__fields">
            <div className="ds-color-picker__format">
              <Select
                value={format}
                options={FORMAT_OPTIONS}
                disabled={disabled}
                aria-label="Format de couleur"
                onChange={(next) => {
                  if (next === 'hex' || next === 'rgb' || next === 'hsb') {
                    setFormat(next)
                    if (next === 'hex') {
                      setHexDraft(hsvToHex(hsvRef.current))
                    }
                  }
                }}
              />
            </div>

            {format === 'hex' ? (
              <TextInput
                id={`${triggerId}-hex`}
                value={hexDraft}
                disabled={disabled}
                placeholder={placeholder}
                spellCheck={false}
                aria-label="Valeur hexadécimale"
                onChange={(event) => {
                  const next = event.target.value
                  setHexDraft(next)
                  if (next.trim().length === 0 || normalizeHex(next)) {
                    commitHex(next)
                  }
                }}
                onBlur={() => {
                  if (hexDraft.trim().length === 0) {
                    return
                  }
                  commitHex(hexDraft)
                  setHexDraft(normalizeHex(hexDraft) ?? hexDraft)
                }}
              />
            ) : (
              <div className="ds-color-picker__channels" data-format={format}>
                {format === 'rgb' ? (
                  <>
                    <InputNumber
                      value={rgb.r}
                      min={0}
                      max={255}
                      step={1}
                      precision={0}
                      controls={false}
                      disabled={disabled}
                      leadingIcon={<span className="ds-color-picker__ch">R</span>}
                      aria-label="Rouge"
                      onChange={(next) => {
                        if (next != null) {
                          commitRgb({ ...rgb, r: next })
                        }
                      }}
                    />
                    <InputNumber
                      value={rgb.g}
                      min={0}
                      max={255}
                      step={1}
                      precision={0}
                      controls={false}
                      disabled={disabled}
                      leadingIcon={<span className="ds-color-picker__ch">G</span>}
                      aria-label="Vert"
                      onChange={(next) => {
                        if (next != null) {
                          commitRgb({ ...rgb, g: next })
                        }
                      }}
                    />
                    <InputNumber
                      value={rgb.b}
                      min={0}
                      max={255}
                      step={1}
                      precision={0}
                      controls={false}
                      disabled={disabled}
                      leadingIcon={<span className="ds-color-picker__ch">B</span>}
                      aria-label="Bleu"
                      onChange={(next) => {
                        if (next != null) {
                          commitRgb({ ...rgb, b: next })
                        }
                      }}
                    />
                  </>
                ) : (
                  <>
                    <InputNumber
                      value={Math.round(hsv.h)}
                      min={0}
                      max={360}
                      step={1}
                      precision={0}
                      controls={false}
                      disabled={disabled}
                      leadingIcon={<span className="ds-color-picker__ch">H</span>}
                      aria-label="Teinte"
                      onChange={(next) => {
                        if (next != null) {
                          commitHsv({ ...hsvRef.current, h: next })
                        }
                      }}
                    />
                    <InputNumber
                      value={Math.round(hsv.s * 100)}
                      min={0}
                      max={100}
                      step={1}
                      precision={0}
                      controls={false}
                      disabled={disabled}
                      leadingIcon={<span className="ds-color-picker__ch">S</span>}
                      aria-label="Saturation"
                      onChange={(next) => {
                        if (next != null) {
                          commitHsv({
                            ...hsvRef.current,
                            s: next / 100,
                          })
                        }
                      }}
                    />
                    <InputNumber
                      value={Math.round(hsv.v * 100)}
                      min={0}
                      max={100}
                      step={1}
                      precision={0}
                      controls={false}
                      disabled={disabled}
                      leadingIcon={<span className="ds-color-picker__ch">B</span>}
                      aria-label="Luminosité"
                      onChange={(next) => {
                        if (next != null) {
                          commitHsv({
                            ...hsvRef.current,
                            v: next / 100,
                          })
                        }
                      }}
                    />
                  </>
                )}
              </div>
            )}
          </div>
        </div>,
            document.body,
          )
        : null}
    </div>
  )
}
