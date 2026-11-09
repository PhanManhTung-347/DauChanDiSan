import { useState } from 'react'
import { Plus } from 'lucide-react'
import { api, upload } from '../../shared/api'
import { useAction, useResource } from '../../shared/hooks'
import { Empty, Feedback, LoadState, Status } from '../../shared/ui'
import type { Experience, Heritage, Page } from '../../shared/types'

export function PartnerExperienceEditor({ id, experiences, reload }: { id: number; experiences?: Experience[]; reload: () => void }) {
  const [selected,setSelected]=useState<number>()
  const places=useResource<Page<Heritage>>(selected===undefined?null:'/di-san?kichThuoc=100')
  const row=experiences?.find(x=>x.traiNghiemId===selected), action=useAction()
  return <section className="surface block">
    <div className="section-heading"><div><p className="eyebrow">TRẢI NGHIỆM CỦA ĐƠN VỊ</p><h2>Nội dung và xét duyệt</h2></div>
      <button className="button outline" onClick={()=>setSelected(0)}><Plus size={17} /> Tạo trải nghiệm</button></div>
    {experiences?.length===0 && <Empty title="Chưa có trải nghiệm. Hãy tạo nội dung đầu tiên." />}
    {experiences?.map(x=><button key={x.traiNghiemId} className={x.traiNghiemId===selected?'queue-row selected':'queue-row'} onClick={()=>setSelected(x.traiNghiemId)}>
      <span><strong>{x.ten}</strong><small>{x.thoiLuongPhut} phút</small></span><Status value={x.trangThai??'NHAP'} /></button>)}
    {selected!==undefined && <div className="block"><LoadState {...places} retry={places.reload} />
      <form className="form-stack" key={selected} onSubmit={e=>{
        e.preventDefault();const f=new FormData(e.currentTarget), title=String(f.get('ten')).trim()
        const slug=row?.slug??title.toLowerCase().normalize('NFD').replace(/\p{Diacritic}/gu,'').replace(/đ/g,'d')
          .replace(/[^a-z0-9]+/g,'-').replace(/^-|-$/g,'').slice(0,120).replace(/-$/,'')
        void action.run(async()=>{
          if(!slug)throw new Error('Tên trải nghiệm cần có chữ hoặc số.')
          const image=f.get('cover')
          const photo=image instanceof File && image.size?await upload(image):row?.anhBiaPath
          await api('/doi-tac/'+id+'/trai-nghiem'+(row?'/'+row.traiNghiemId:''),row?'PUT':'POST',{
            ten:title,slug:row?slug:slug+'-'+crypto.randomUUID().slice(0,8),moTa:f.get('moTa'),diSanId:Number(f.get('diSanId')),
            diaDiemTapTrung:f.get('diaDiemTapTrung'),thoiLuongPhut:Number(f.get('thoiLuongPhut')),
            giaMoiNguoi:f.get('giaMoiNguoi')===''?null:Number(f.get('giaMoiNguoi')),anhBiaPath:photo,videoPath:row?.videoPath,guiDuyet:f.has('guiDuyet'),
          });reload();setSelected(undefined)
        },f.has('guiDuyet')?'Đã gửi trải nghiệm chờ duyệt.':'Đã lưu bản nháp.')
      }}>
        <h3>{row?'Sửa '+row.ten:'Tạo trải nghiệm mới'}</h3>
        <div className="form-grid"><label>Tên trải nghiệm<input name="ten" maxLength={180} defaultValue={row?.ten} required /></label>
          <label>Di sản liên quan<select name="diSanId" defaultValue={row?.diSanId??''} required><option value="">Chọn di sản</option>
            {row && !places.data?.muc.some(x=>x.diSanId===row.diSanId) && <option value={row.diSanId}>Giữ di sản hiện tại</option>}
            {places.data?.muc.map(x=><option key={x.diSanId} value={x.diSanId}>{x.ten}</option>)}</select></label></div>
        <label>Mô tả<textarea name="moTa" rows={4} maxLength={15000} defaultValue={row?.moTa} required /></label>
        <label>Điểm tập trung<input name="diaDiemTapTrung" maxLength={250} defaultValue={row?.diaDiemTapTrung} /></label>
        <div className="form-grid"><label>Thời lượng (phút)<input name="thoiLuongPhut" type="number" min={1} max={1440} defaultValue={row?.thoiLuongPhut??60} required /></label>
          <label>Giá mỗi người (đồng)<input name="giaMoiNguoi" type="number" min={0} max={999999999999} defaultValue={row?.giaMoiNguoi} /></label></div>
        <label>Ảnh bìa<input name="cover" type="file" accept="image/jpeg,image/png,image/webp" /></label>
        <label className="check"><input type="checkbox" name="guiDuyet" defaultChecked={row?.trangThai!=='NHAP'} /> Gửi quản trị viên duyệt</label>
        {row?.trangThai==='CONG_KHAI' && <small>Trải nghiệm đang công khai sẽ về chờ duyệt hoặc bản nháp sau khi lưu.</small>}
        <div className="inline"><button className="button primary" disabled={action.busy}>Lưu trải nghiệm</button>
          <button type="button" className="button quiet" onClick={()=>setSelected(undefined)}>Đóng</button></div>
      </form></div>}
    <Feedback error={action.error} message={action.message} />
  </section>
}
