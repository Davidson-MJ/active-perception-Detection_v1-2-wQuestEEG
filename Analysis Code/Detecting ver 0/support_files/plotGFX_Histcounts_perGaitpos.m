function plotGFX_Histcounts_perGaitpos(dataIN,cfg)


yaxistitle= cfg.ytitle;
datadir = cfg.datadir;
headData= cfg.headY;

% ----
% dataIN  = the GFX structure (either targ pos or resp pos, per ppant).
% head Data = GFX head data for overlaying on plot.
% ---
% using the input data specified, pot a 2 x 2 subplot, showing
% the raw count per data point (100 samps for single gait, 200 for double
% gait), and the binned count alongside.
%

usecols = {[0 .7 0], [.7 0 0], [.7 0 .7]};
gaittypes = {'single gait' , 'double gait'};
nsubs = size(dataIN,1);
for nGaits_toPlot=1:2
    %% figure:
    
    figure(nGaits_toPlot); clf; set(gcf, 'color', 'w', 'units', 'normalized', 'position', [0 0 .9  .9]);
    pc=1; % plot count
    
    %fill these variables based on nGaits to plot:
    [plotData,binnedcountsResp,binnedcountsShuff,...
        rawcountsResp,plotHead]= deal([]);
    
    
    %for each foot as starting point
    for iLR=1:3
        rawcountsResp=[];
        
        if nGaits_toPlot==1
            %Head pos data:
            
            %per ppant, extract info:
            for ip=1:size(dataIN,1)
                % without binning, counts per % gait:
                rawcountsResp(ip,:) = dataIN(ip,iLR).gc_counts;
                binnedcountsResp(ip,:)=dataIN(ip,iLR).gc_binned_counts;
                % store the 95% quantile 
                binnedcountsShuff(ip,1:3,:) =quantile(dataIN(ip,iLR).gc_binned_counts_shuff, [.05 .5 .95]);
                plotHead(ip,:) = headData(ip).gc;
            end
            
            %             pidx= pidx1;
            midp=50;
            pidx=cfg.pidx1;
            
            ftnames= {'L-R', 'R-L', 'combined'};
        else
            
            for ip=1:size(dataIN,1)
                
                rawcountsResp(ip,:) = dataIN(ip,iLR).doubgc_counts;
                binnedcountsResp(ip,:)=dataIN(ip,iLR).doubgc_binned_counts;
                binnedcountsShuff(ip,1:3,:) =quantile(dataIN(ip,iLR).doubgc_binned_counts_shuff, [.05, .5 , .95]);

                plotHead(ip,:) = headData(ip).doubgc;
            end
            midp=100;
            pidx= cfg.pidx2;
            
            ftnames= {'L-R-L', 'R-L-R', 'combined'};
        end
        
        %% plot each data type:
        for plotd=1:2
            switch plotd
                case 1
                    useD= rawcountsResp; 
                    xvec=1:length(useD);
                case 2
                    useD= binnedcountsResp; 
%                     xv=linspace(1,pidx,size(binnedcountsResp,2)); % length 7
                     %x axis:          %approx centre point of the binns.
                mdiff = round(mean(diff(pidx)./2));
                xvec = pidx(1:end-1) + mdiff;
            end
            % if normON
            if (isfield(cfg, 'norm') && cfg.norm==1)
                
                pM = nanmean(useD,2);
                meanVals= repmat(pM, 1, size(useD,2));
                
                
                if strcmp(cfg.normtype, 'absolute')
                    data = useD - meanVals;
                elseif strcmp(cfg.normtype, 'relative')
                    data = useD  ./ meanVals;
                    data=data-1;
                elseif strcmp(cfg.normtype, 'relchange')
                    data = (useD  - meanVals) ./ meanVals;
                elseif strcmp(cfg.normtype, 'normchange')
                    data = (useD  - meanVals) ./ (useD + meanVals);
                elseif strcmp(cfg.normtype, 'db')
                    data = 10*log10(useD  ./ meanVals);
                end
                
                useD= data;
                
            end
            % first just plot the counts per point:
            %use nbins to match the binned analysis:
            subplot(3,2,pc)
            hs=bar(xvec, nanmean(useD,1));
            hs.FaceColor = usecols{iLR};
            
            hold on;
            stE= CousineauSEM(useD);
            if plotd==2
                errorbar(xvec, nanmean(useD,1), stE,...
                    'linestyle', 'none', 'color', 'k', 'linew', 3)
                
                %% also add shuffle data.
                if ~cfg.norm && cfg.plotShuff==1
                    for iCV=[1,3]
                        tmp = squeeze( binnedcountsShuff(:,iCV,:));
                        mP = nanmean(tmp,1);
                        stE= CousineauSEM(tmp);
                        shadedErrorBar(xv, mP, stE, 'k');
                    end
                end
                
                
                %restrict yrange?
                %adjust ylims to capture 2*SD of data points.
                if cfg.norm==0
                   gM= nanmean(useD,1);
                    sdrange = max(gM) - min(gM);
                    ylim([min(gM)-2*sdrange max(gM)+2*sdrange])
                else
%                     ylim(cfg.ylims)
                end
            end
            %%
            ylabel(['Onsets [counts]'])
            xlabel([cfg.ytitle ' onset in gait %'])
            %
            
            set(gca,'fontsize', 15, 'xtick', [1, midp, pidx(end)], 'XTickLabels', {'0', '50', '100%'})
            
            yyaxis right
            ph=plot(nanmean(plotHead,1), ['k-o']); hold on
            set(gca,'ytick', []);
            title(['GFX ' ftnames{iLR} ' N=' num2str(nsubs)], 'interpreter', 'none')
            pc=pc+1;
        end
    end
    
cd([datadir filesep  'Figures' filesep yaxistitle ' onset distribution'])
%%
print(['GFX ' yaxistitle ' counts per position within ' gaittypes{nGaits_toPlot} ],'-dpng');
    
end
end % function end


